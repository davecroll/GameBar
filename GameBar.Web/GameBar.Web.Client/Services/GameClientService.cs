using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GameBar.Game.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace GameBar.Web.Client.Services;

public class GameClientService
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    private HubConnection? _connection;
    private readonly List<InputCommand> _pendingInputs = new();
    private readonly Dictionary<string, PlayerState> _players = new();

    private long _nextInputSequence = 0;
    private string? _localPlayerId;

    public GameClientService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
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

        // simple client-side prediction: apply instantly
        if (_localPlayerId is not null && _players.TryGetValue(_localPlayerId, out var player))
        {
            ApplyInputLocally(player, input, TimeSpan.FromMilliseconds(50));
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

        if (_localPlayerId is not null && snapshot.LastProcessedInputSequenceByPlayer.TryGetValue(_localPlayerId, out var ackSequence))
        {
            _pendingInputs.RemoveAll(i => i.ClientInputSequence <= ackSequence);
        }

        _ = RenderAsync();
    }

    private void ApplyInputLocally(PlayerState player, InputCommand input, TimeSpan dt)
    {
        var dtSeconds = (float)dt.TotalSeconds;
        float dx = 0, dy = 0;
        if (input.Up) dy -= 1;
        if (input.Down) dy += 1;
        if (input.Left) dx -= 1;
        if (input.Right) dx += 1;

        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length > 0)
        {
            dx /= length;
            dy /= length;
        }

        const float speed = 5f;

        player.X += dx * speed * dtSeconds;
        player.Y += dy * speed * dtSeconds;
    }

    public async Task InitPixiAsync(ElementReference container)
    {
        await _jsRuntime.InvokeVoidAsync("gameBarPixi.init", container);
    }

    public async Task RenderAsync()
    {
        var players = _players.Values.Select(p => new
        {
            id = p.PlayerId,
            x = p.X,
            y = p.Y
        }).ToArray();

        await _jsRuntime.InvokeVoidAsync("gameBarPixi.render", new { players });
    }
}
