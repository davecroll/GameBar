using System;
using System.Net.Http;
using System.Text.Json;
using GameBar.Game.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace GameBar.Client.Services;

public class GameClientService
{
    private readonly NavigationManager _navigationManager;
    private readonly GameBarPixiInterop _pixi;

    private HubConnection? _connection;

    private long _nextInputSequence;
    private string? _localPlayerId;

    // Server simulation tick duration, in ms. Must match server's fixed step.
    private const int ServerTickDurationMs = 25;

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

        _connection.On<StateSnapshot>("ReceiveSnapshot", async snapshot =>
        {
            await HandleSnapshotAsync(snapshot);
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

    private async Task HandleSnapshotAsync(StateSnapshot snapshot)
    {
        var data = new
        {
            serverTick = snapshot.ServerTick,
            players = snapshot.Players.Select(kvp => new
            {
                id = kvp.Key,
                x = kvp.Value.X,
                y = kvp.Value.Y,
                movementStateName = kvp.Value.MovementStateName,
                movementStateStartTick = kvp.Value.MovementStateStartTick,
                actionStateName = kvp.Value.ActionStateName,
                actionStateStartTick = kvp.Value.ActionStateStartTick,
            }).ToArray()
        };

        await _pixi.PushSnapshotAsync(data);
    }

    // Load the ESM Pixi module once and call its init
    public async Task InitPixiAsync(ElementReference container)
    {
        await _pixi.InitAsync(container);
        await _pixi.LoadAssetsAsync();

        // Push animation manifest to JS
        var manifest = _manifest ?? AnimationManifest.Default;
        var manifestData = new
        {
            tickDurationMs = ServerTickDurationMs,
            states = manifest.States.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    assetKey = kvp.Value.AssetKey,
                    frameCount = kvp.Value.FrameCount,
                    frameWidth = kvp.Value.FrameWidth,
                    frameHeight = kvp.Value.FrameHeight,
                    frameDurationMs = kvp.Value.FrameDurationMs,
                    loop = kvp.Value.Loop,
                })
        };
        await _pixi.SetManifestAsync(manifestData);

        await _pixi.StartLoopAsync();
    }

    public async Task StopLoopAsync()
    {
        await _pixi.StopLoopAsync();
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
