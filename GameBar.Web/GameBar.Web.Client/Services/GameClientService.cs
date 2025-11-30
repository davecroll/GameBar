using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using GameBar.Game.Models;
using GameBar.Game.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace GameBar.Web.Client.Services;

public class GameClientService
{
    private readonly NavigationManager _navigationManager;
    private readonly GameBarPixiInterop _pixi;

    private HubConnection? _connection;
    private readonly List<InputCommand> _pendingInputs = new();
    private readonly Dictionary<string, PlayerState> _players = new();

    private long _nextInputSequence;
    private string? _localPlayerId;

    private long _latestServerTick;
    private DateTimeOffset _lastSnapshotReceivedAt;

    private const int TickDurationMs = 50; // must match server
    private const int IdleFrameDurationMs = 250;
    private const int RunFrameDurationMs = 100; // faster running animation

    private AnimationManifest? _manifest;

    public GameClientService(NavigationManager navigationManager, GameBarPixiInterop pixi)
    {
        _navigationManager = navigationManager;
        _pixi = pixi;
    }

    public async Task InitializeAsync()
    {
        if (_connection is not null)
        {
            return;
        }

        // Load manifest
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(_navigationManager.BaseUri) };
            var json = await http.GetStringAsync("animationManifest.json");
            _manifest = JsonSerializer.Deserialize<AnimationManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            // fallback if manifest load fails
            _manifest = AnimationManifest.Default;
        }

        var hubUrl = new Uri(new Uri(_navigationManager.BaseUri), "hubs/game");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<StateSnapshot>("ReceiveSnapshot", snapshot =>
        {
            HandleSnapshot(snapshot);
            return Task.CompletedTask;
        });

        await _connection.StartAsync();

        _localPlayerId = _connection.ConnectionId;
    }

    public async Task SendInputAsync(bool up, bool down, bool left, bool right)
    {
        if (_connection is null)
        {
            return;
        }

        var input = new InputCommand
        {
            PlayerId = _localPlayerId ?? string.Empty,
            ClientInputSequence = Interlocked.Increment(ref _nextInputSequence),
            ClientTick = 0,
            Up = up,
            Down = down,
            Left = left,
            Right = right
        };

        _pendingInputs.Add(input);

        await _connection.SendAsync("SendInput", input);

        // simple client-side prediction: apply instantly using shared input processing
        if (_localPlayerId is not null && _players.TryGetValue(_localPlayerId, out var player))
        {
            ApplyInputLocally(player, input, TimeSpan.FromMilliseconds(TickDurationMs));
            await RenderAsync();
        }
    }

    private void HandleSnapshot(StateSnapshot snapshot)
    {
        _players.Clear();
        foreach (var kvp in snapshot.Players)
        {
            _players[kvp.Key] = kvp.Value;
        }

        _latestServerTick = snapshot.ServerTick;
        _lastSnapshotReceivedAt = DateTimeOffset.UtcNow;

        if (_localPlayerId is not null && snapshot.LastProcessedInputSequenceByPlayer.TryGetValue(_localPlayerId, out var ackSequence))
        {
            _pendingInputs.RemoveAll(i => i.ClientInputSequence <= ackSequence);
        }

        _ = RenderAsync();
    }

    private void ApplyInputLocally(PlayerState player, InputCommand input, TimeSpan dt)
    {
        var dtSeconds = (float)dt.TotalSeconds;
        var (vx, vy) = InputProcessing.InputToVelocity(input);
        player.X += vx * dtSeconds;
        player.Y += vy * dtSeconds;
    }

    // Load the ESM Pixi module once and call its init
    public async Task InitPixiAsync(ElementReference container)
    {
        await _pixi.InitAsync(container);
        await _pixi.LoadAssetsAsync();
    }

    private long GetCurrentTick()
    {
        var tickMs = _manifest?.TickDurationMs ?? TickDurationMs;
        var elapsedMs = (DateTimeOffset.UtcNow - _lastSnapshotReceivedAt).TotalMilliseconds;
        var addTicks = (long)Math.Floor(elapsedMs / tickMs);
        return _latestServerTick + addTicks;
    }

    private async Task RenderAsync()
    {
        var currentTick = GetCurrentTick();
        var manifest = _manifest ?? AnimationManifest.Default;

        var players = _players.Values.Select(p =>
        {
            // Choose layer: if ActionStateName present -> action, else movement
            var stateName = string.IsNullOrEmpty(p.ActionStateName) ? p.MovementStateName : p.ActionStateName!;
            var startTick = string.IsNullOrEmpty(p.ActionStateName) ? p.MovementStateStartTick : p.ActionStateStartTick ?? p.MovementStateStartTick;
            var meta = manifest.States.TryGetValue(stateName, out var m) ? m : manifest.States["Idle"];

            var stepTicks = Math.Max(1, meta.FrameDurationMs / manifest.TickDurationMs);
            var frames = meta.FrameCount;
            int frameIndex = 0;
            var delta = currentTick - startTick;
            if (delta >= 0)
            {
                var steps = (int)(delta / stepTicks);
                frameIndex = meta.Loop ? steps % frames : Math.Min(frames - 1, steps);
            }

            var animKey = meta.AssetKey;
            return new PixiPlayer(p.PlayerId, p.X, p.Y, frameIndex, animKey);
        }).ToArray();

        await _pixi.RenderAsync(players);
    }

    // Gracefully destroy Pixi
    public async Task DestroyAsync()
    {
        await _pixi.DestroyAsync();
    }

    private sealed class AnimationManifest
    {
        public int TickDurationMs { get; set; } = 50;
        public Dictionary<string, AnimationMeta> States { get; set; } = new();

        public static AnimationManifest Default => new AnimationManifest
        {
            TickDurationMs = 50,
            States = new Dictionary<string, AnimationMeta>
            {
                { "Idle", new AnimationMeta { AssetKey = "idle", FrameCount = 10, FrameWidth = 48, FrameHeight = 48, FrameDurationMs = 250, Loop = true } },
                { "Run",  new AnimationMeta { AssetKey = "run",  FrameCount = 8,  FrameWidth = 48, FrameHeight = 48, FrameDurationMs = 100, Loop = true } },
            }
        };
    }

    private sealed class AnimationMeta
    {
        public string AssetKey { get; set; } = "idle";
        public int FrameCount { get; set; }
        public int FrameWidth { get; set; } = 48;
        public int FrameHeight { get; set; } = 48;
        public int FrameDurationMs { get; set; }
        public bool Loop { get; set; } = true;
    }
}
