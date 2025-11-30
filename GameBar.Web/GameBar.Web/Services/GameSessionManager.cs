using GameBar.Game.Models;
using GameBar.Game.Simulation;
using GameBar.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameBar.Web.Services;

public class GameSessionManager
{
    public const string DefaultGroupName = "game-default";

    private readonly IGameSimulation _simulation;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameSessionManager> _logger;

    private readonly Dictionary<string, long> _lastProcessedInputSequenceByPlayer = new();

    public GameSessionManager(IGameSimulation simulation, IHubContext<GameHub> hubContext, ILogger<GameSessionManager> logger)
    {
        _simulation = simulation;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task AddPlayerAsync(string connectionId)
    {
        _simulation.AddPlayer(connectionId);
        _logger.LogInformation("Player {PlayerId} joined", connectionId);
        return Task.CompletedTask;
    }

    public Task RemovePlayerAsync(string connectionId)
    {
        _simulation.RemovePlayer(connectionId);
        _logger.LogInformation("Player {PlayerId} left", connectionId);
        return Task.CompletedTask;
    }

    public void EnqueueInput(string connectionId, InputCommand input)
    {
        _simulation.EnqueueInput(connectionId, input);
        _lastProcessedInputSequenceByPlayer[connectionId] = input.ClientInputSequence;
    }

    public async Task TickAsync(TimeSpan dt, CancellationToken cancellationToken)
    {
        _simulation.Update(dt);

        var snapshot = new StateSnapshot
        {
            GameId = _simulation.State.GameId,
            ServerTick = _simulation.State.Tick,
            Players = new Dictionary<string, PlayerSnapshot>(_simulation.State.Players),
            LastProcessedInputSequenceByPlayer = new Dictionary<string, long>(_lastProcessedInputSequenceByPlayer)
        };

        await _hubContext.Clients.Group(DefaultGroupName).SendAsync("ReceiveSnapshot", snapshot, cancellationToken);
    }
}
