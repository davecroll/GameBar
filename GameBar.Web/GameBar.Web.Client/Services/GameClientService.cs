using System;
using System.Net.Http;
using System.Text.Json;
using GameBar.Game.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace GameBar.Web.Client.Services;

public class GameClientService
{
    private readonly NavigationManager _navigationManager;
    private readonly GameBarPixiInterop _pixi;

    private HubConnection? _connection;

    // Authoritative server snapshot cache
    private readonly Dictionary<string, PlayerSnapshot> _players = new();

    private long _nextInputSequence;
    private string? _localPlayerId;

    private long _latestServerTick;

    // Server simulation tick duration, in ms. Must match server's fixed step.
    private const int ServerTickDurationMs = 25;

    private AnimationManifest? _manifest;

    private DotNetObjectReference<GameClientService>? _dotNetRef;

    // Latest input state for local player (mirrors Game.razor flags)
    private bool _inputUp, _inputDown, _inputLeft, _inputRight, _inputAttack, _inputJump;

    public GameClientService(NavigationManager navigationManager, GameBarPixiInterop pixi)
    {
        _navigationManager = navigationManager;
        _pixi = pixi;
    }

    // Called by Game.razor when input state changes
    public Task UpdateLocalInputStateAsync(bool up, bool down, bool left, bool right, bool attack, bool jump)
    {
        _inputUp = up;
        _inputDown = down;
        _inputLeft = left;
        _inputRight = right;
        _inputAttack = attack;
        _inputJump = jump;
        return Task.CompletedTask;
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
            using var http = new HttpClient();
            http.BaseAddress = new Uri(_navigationManager.BaseUri);
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

    public async Task SendInputAsync(bool up, bool down, bool left, bool right, bool attack, bool jump)
    {
        if (_connection is null) return;
        var input = new InputCommand
        {
            PlayerId = _localPlayerId ?? string.Empty,
            ClientInputSequence = Interlocked.Increment(ref _nextInputSequence),
            ClientTick = 0,
            Up = up,
            Down = down,
            Left = left,
            Right = right,
            Attack = attack,
            Jump = jump
        };
        await _connection.SendAsync("SendInput", input);
    }

    private void HandleSnapshot(StateSnapshot snapshot)
    {
        _players.Clear();
        foreach (var kvp in snapshot.Players)
        {
            _players[kvp.Key] = kvp.Value;
        }

        _latestServerTick = snapshot.ServerTick;
    }

    // Load the ESM Pixi module once and call its init
    public async Task InitPixiAsync(ElementReference container)
    {
        await _pixi.InitAsync(container);
        await _pixi.LoadAssetsAsync();

        _dotNetRef ??= DotNetObjectReference.Create(this);
        await _pixi.SetDotNetRefAsync(_dotNetRef);
    }

    public async Task StartLoopAsync()
    {
        await _pixi.StartLoopAsync();
    }

    public async Task StopLoopAsync()
    {
        await _pixi.StopLoopAsync();
    }

    [JSInvokable]
    public Task<PixiPlayer[]> GetRenderPlayersAsync()
    {
        // Render purely from latest authoritative server snapshot; no client-side prediction.
        var manifest = _manifest ?? AnimationManifest.Default;

        // Convert authoritative server tick into elapsed milliseconds on the server timeline.
        var currentServerTimeMs = _latestServerTick * ServerTickDurationMs;

        var renderPlayers = new List<PixiPlayer>();

        foreach (var (id, source) in _players)
        {
            var stateName = string.IsNullOrEmpty(source.ActionStateName)
                ? source.MovementStateName
                : source.ActionStateName!;

            if (!manifest.States.TryGetValue(stateName, out var meta))
            {
                meta = manifest.States["Idle"];
            }

            // Use the appropriate state start tick from the authoritative snapshot.
            var startTick = string.IsNullOrEmpty(source.ActionStateName)
                ? source.MovementStateStartTick
                : source.ActionStateStartTick ?? source.MovementStateStartTick;

            var startTimeMs = startTick * ServerTickDurationMs;
            var elapsedMs = Math.Max(0, currentServerTimeMs - startTimeMs);

            var frameDurationMs = Math.Max(1, meta.FrameDurationMs);
            var frames = Math.Max(1, meta.FrameCount);

            var steps = (int)(elapsedMs / frameDurationMs);
            var frameIndex = meta.Loop
                ? steps % frames
                : Math.Min(frames - 1, steps);

            renderPlayers.Add(new PixiPlayer(id, source.X, source.Y, frameIndex, meta.AssetKey, meta.FrameWidth, meta.FrameHeight));
        }

        return Task.FromResult(renderPlayers.ToArray());
    }

    // Gracefully destroy Pixi
    public async Task DestroyAsync()
    {
        await _pixi.DestroyAsync();
    }

    private sealed class AnimationManifest
    {
        // Per-state animations specify their own FrameDurationMs; this property remains as a
        // generic client-side default but does not drive frame timing directly.
        public Dictionary<string, AnimationMeta> States { get; set; } = new();

        public static AnimationManifest Default => new AnimationManifest
        {
            States = new Dictionary<string, AnimationMeta>
            {
                { "Idle", new AnimationMeta { AssetKey = "idle", FrameCount = 10, FrameWidth = 48, FrameHeight = 48, FrameDurationMs = 95, Loop = true } },
                { "Run",  new AnimationMeta { AssetKey = "run",  FrameCount = 8,  FrameWidth = 48, FrameHeight = 48, FrameDurationMs = 80, Loop = true } },
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
