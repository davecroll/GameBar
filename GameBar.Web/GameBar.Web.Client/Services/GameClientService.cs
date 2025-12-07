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

    // Authoritative server snapshot cache (for reconciliation / remote players)
    private readonly Dictionary<string, PlayerSnapshot> _players = new();

    private long _nextInputSequence;
    private string? _localPlayerId;

    private long _latestServerTick;
    private DateTimeOffset _lastSnapshotReceivedAt;

    private const int TickDurationMs = 50; // must match server

    private AnimationManifest? _manifest;

    private DotNetObjectReference<GameClientService>? _dotNetRef;

    // Local client-side simulation for prediction
    private readonly IGameSimulation _localSimulation = new GameSimulation();
    private DateTimeOffset _lastLocalSimUpdate = DateTimeOffset.UtcNow;
    private double _accumulatedMs;

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
        if (!string.IsNullOrEmpty(_localPlayerId))
        {
            _localSimulation.AddPlayer(_localPlayerId);
        }
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
        _pendingInputs.Add(input);
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
        _lastSnapshotReceivedAt = DateTimeOffset.UtcNow;

        // Reconcile local simulation with authoritative snapshot
        if (_localPlayerId is not null && _players.TryGetValue(_localPlayerId, out var localAuthoritative))
        {
            // Snap local simulated player to server position/velocity to avoid long-term drift
            if (_localSimulation.State.Players.TryGetValue(_localPlayerId, out var localSimPlayer))
            {
                localSimPlayer.X = localAuthoritative.X;
                localSimPlayer.Y = localAuthoritative.Y;
                localSimPlayer.VX = localAuthoritative.VX;
                localSimPlayer.VY = localAuthoritative.VY;
                localSimPlayer.IsGrounded = localAuthoritative.IsGrounded;
                localSimPlayer.GroundY = localAuthoritative.GroundY;
                localSimPlayer.MovementState = localAuthoritative.MovementState;
                localSimPlayer.MovementStateName = localAuthoritative.MovementStateName;
                localSimPlayer.MovementStateStartTick = localAuthoritative.MovementStateStartTick;
                localSimPlayer.ActionStateName = localAuthoritative.ActionStateName;
                localSimPlayer.ActionStateStartTick = localAuthoritative.ActionStateStartTick;
            }
            else
            {
                // If local sim does not have the player yet, add from snapshot
                _localSimulation.AddPlayer(_localPlayerId);
                if (_localSimulation.State.Players.TryGetValue(_localPlayerId, out var created))
                {
                    created.X = localAuthoritative.X;
                    created.Y = localAuthoritative.Y;
                    created.VX = localAuthoritative.VX;
                    created.VY = localAuthoritative.VY;
                    created.IsGrounded = localAuthoritative.IsGrounded;
                    created.GroundY = localAuthoritative.GroundY;
                    created.MovementState = localAuthoritative.MovementState;
                    created.MovementStateName = localAuthoritative.MovementStateName;
                    created.MovementStateStartTick = localAuthoritative.MovementStateStartTick;
                    created.ActionStateName = localAuthoritative.ActionStateName;
                    created.ActionStateStartTick = localAuthoritative.ActionStateStartTick;
                }
            }
        }

        if (_localPlayerId is not null && snapshot.LastProcessedInputSequenceByPlayer.TryGetValue(_localPlayerId, out var ackSequence))
        {
            _pendingInputs.RemoveAll(i => i.ClientInputSequence <= ackSequence);
        }
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
        _lastLocalSimUpdate = DateTimeOffset.UtcNow;
        await _pixi.StartLoopAsync();
    }

    public async Task StopLoopAsync()
    {
        await _pixi.StopLoopAsync();
    }

    [JSInvokable]
    public Task<PixiPlayer[]> GetRenderPlayersAsync()
    {
        // Advance local simulation using a fixed-step loop to match server tick rate
        var now = DateTimeOffset.UtcNow;
        var dt = now - _lastLocalSimUpdate;
        if (dt.TotalMilliseconds < 0)
        {
            dt = TimeSpan.Zero;
        }
        _lastLocalSimUpdate = now;

        _accumulatedMs += dt.TotalMilliseconds;
        var fixedStepMs = _manifest?.TickDurationMs ?? TickDurationMs; // typically 50ms

        // Enqueue latest input snapshot for local player before stepping
        if (!string.IsNullOrEmpty(_localPlayerId))
        {
            var input = new InputCommand
            {
                PlayerId = _localPlayerId,
                Up = _inputUp,
                Down = _inputDown,
                Left = _inputLeft,
                Right = _inputRight,
                Attack = _inputAttack,
                Jump = _inputJump
            };
            _localSimulation.EnqueueInput(_localPlayerId, input);
        }

        while (_accumulatedMs >= fixedStepMs)
        {
            _localSimulation.Update(TimeSpan.FromMilliseconds(fixedStepMs));
            _accumulatedMs -= fixedStepMs;
        }

        var currentTick = _localSimulation.State.Tick;
        var manifest = _manifest ?? AnimationManifest.Default;

        // Use local simulation state for local player, server snapshots for others
        var renderPlayers = new List<PixiPlayer>();

        foreach (var kvp in _players)
        {
            var id = kvp.Key;
            var serverPlayer = kvp.Value;
            PlayerSnapshot source;

            if (id == _localPlayerId && _localSimulation.State.Players.TryGetValue(id, out var localSimPlayer))
            {
                source = localSimPlayer;
            }
            else
            {
                source = serverPlayer;
            }

            var stateName = string.IsNullOrEmpty(source.ActionStateName) ? source.MovementStateName : source.ActionStateName!;
            var startTick = string.IsNullOrEmpty(source.ActionStateName) ? source.MovementStateStartTick : source.ActionStateStartTick ?? source.MovementStateStartTick;
            if (!manifest.States.TryGetValue(stateName, out var meta))
            {
                meta = manifest.States["Idle"];
            }

            var stepTicks = Math.Max(1, meta.FrameDurationMs / manifest.TickDurationMs);
            var frames = meta.FrameCount;
            var deltaTicks = currentTick - startTick;
            var frameIndex = 0;
            if (deltaTicks >= 0)
            {
                var steps = (int)(deltaTicks / stepTicks);
                frameIndex = meta.Loop ? steps % frames : Math.Min(frames - 1, steps);
            }

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
        public int TickDurationMs { get; set; } = 50;
        public Dictionary<string, AnimationMeta> States { get; set; } = new();

        public static AnimationManifest Default => new AnimationManifest
        {
            TickDurationMs = 50,
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
