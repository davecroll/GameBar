using GameBar.Game.Models;
using GameBar.Game.Simulation;

namespace GameBar.Game.Simulation;

public class GameSimulation : IGameSimulation
{
    public GameState State { get; } = new();

    private readonly Dictionary<string, InputCommand> _latestInputs = new();

    private const int DebounceTicks = 2; // require sustained state for N ticks
    private readonly Dictionary<string, (MovementState state, long sinceTick)> _stateCandidate = new();

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
                LastActivityTick = State.Tick
            };
        }
    }

    public void RemovePlayer(string playerId)
    {
        State.Players.Remove(playerId);
        _latestInputs.Remove(playerId);
        _stateCandidate.Remove(playerId);
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
            if (!_latestInputs.TryGetValue(playerId, out var input))
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

            var desired = (Math.Abs(player.VX) < 0.0001f && Math.Abs(player.VY) < 0.0001f)
                ? MovementState.Idle
                : MovementState.Running;

            if (!_stateCandidate.TryGetValue(playerId, out var cand) || cand.state != desired)
            {
                _stateCandidate[playerId] = (desired, State.Tick);
            }
            else
            {
                var sustained = State.Tick - cand.sinceTick;
                if (sustained >= DebounceTicks && player.MovementState != desired)
                {
                    player.MovementState = desired;
                    if (desired == MovementState.Idle)
                    {
                        player.IdleStartTick = State.Tick;
                    }
                    else
                    {
                        player.RunningStartTick = State.Tick;
                    }
                    player.LastActivityTick = State.Tick;
                }
            }
        }

        State.Tick++;
        State.LastUpdated = DateTimeOffset.UtcNow;
    }
}
