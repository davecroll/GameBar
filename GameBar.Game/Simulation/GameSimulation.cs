using GameBar.Game.Models;
using GameBar.Game.Simulation.PlayerFsm;

namespace GameBar.Game.Simulation;

public class GameSimulation : IGameSimulation
{
    public GameState State { get; } = new();

    private readonly Dictionary<string, InputCommand> _latestInputs = new();

    private readonly MovementStateMachine _movementFsm = new();
    private readonly ActionStateMachine _actionFsm = new(tickDurationMs: 50, jabFrameCount: 10, jabFrameDurationMs: 80);

    public void AddPlayer(string playerId)
    {
        if (!State.Players.ContainsKey(playerId))
        {
            State.Players[playerId] = new PlayerState
            {
                PlayerId = playerId,
                X = 0,
                Y = 0,
                VX = 0,
                VY = 0,
                MovementState = MovementState.Idle,
                IdleStartTick = State.Tick,
                RunningStartTick = 0,
                LastActivityTick = State.Tick,
                MovementStateName = "Idle",
                MovementStateStartTick = State.Tick
            };
        }
    }

    public void RemovePlayer(string playerId)
    {
        State.Players.Remove(playerId);
        _latestInputs.Remove(playerId);
    }

    public void EnqueueInput(string playerId, InputCommand input)
    {
        _latestInputs[playerId] = input;
    }

    public void Update(TimeSpan dt)
    {
        var dtSeconds = (float)dt.TotalSeconds;

        foreach (var (playerId, player) in State.Players.ToArray())
        {
            _latestInputs.TryGetValue(playerId, out var input);

            if (input is null)
            {
                player.VX = 0;
                player.VY = 0;
            }
            else
            {
                (player.VX, player.VY) = InputProcessing.InputToVelocity(input);
            }

            player.X += player.VX * dtSeconds;
            player.Y += player.VY * dtSeconds;

            _movementFsm.Evaluate(player, State.Tick);
            _actionFsm.Evaluate(player, input, State.Tick);
        }

        State.Tick++;
        State.LastUpdated = DateTimeOffset.UtcNow;
    }
}
