using System;
using System.Linq;
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
        var elapsedMs = (DateTimeOffset.UtcNow - _lastSnapshotReceivedAt).TotalMilliseconds;
        var addTicks = (long)Math.Floor(elapsedMs / TickDurationMs);
        return _latestServerTick + addTicks;
    }

    private async Task RenderAsync()
    {
        var currentTick = GetCurrentTick();

        int idleStepTicks = Math.Max(1, IdleFrameDurationMs / TickDurationMs);
        int runStepTicks = Math.Max(1, RunFrameDurationMs / TickDurationMs);

        var players = _players.Values.Select(p =>
        {
            string anim = p.MovementState == MovementState.Running ? "run" : "idle";
            long startTick = p.MovementState == MovementState.Running ? p.RunningStartTick : p.IdleStartTick;
            int frames = anim == "run" ? 8 : 10;
            int stepTicks = anim == "run" ? runStepTicks : idleStepTicks;
            int frameIndex = 0;
            if (startTick > 0)
            {
                var delta = currentTick - startTick;
                if (delta >= 0)
                {
                    frameIndex = (int)((delta / stepTicks) % frames);
                }
            }
            return new PixiPlayer(p.PlayerId, p.X, p.Y, frameIndex, anim);
        }).ToArray();

        await _pixi.RenderAsync(players);
    }

    // Gracefully destroy Pixi
    public async Task DestroyAsync()
    {
        await _pixi.DestroyAsync();
    }
}
