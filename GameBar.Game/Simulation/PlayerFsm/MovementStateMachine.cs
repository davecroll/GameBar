using GameBar.Game.Models;
using GameBar.Game.Simulation.PlayerFsm.Movement;

namespace GameBar.Game.Simulation.PlayerFsm;

/// <summary>
/// Minimal state machine for the Movement layer with Idle and Run.
/// </summary>
public sealed class MovementStateMachine
{
    private readonly IPlayerState[] _states = [ new IdleState(), new RunState(), new FallState(), new JumpState() ];

    private const int DebounceTicks = 1; // quicker state transitions for jump/fall responsiveness
    private readonly Dictionary<string, (string desiredName, long sinceTick)> _candidates = new();

    public void Evaluate(PlayerSnapshot player, long tick)
    {
        // Initialize name on first evaluation
        if (string.IsNullOrEmpty(player.MovementStateName))
        {
            player.MovementStateName = player.IsGrounded ? "Idle" : "Fall";
            player.MovementStateStartTick = tick;
        }

        // Determine desired state from available states based on CanEnter
        IPlayerState desired = _states
            .Where(s => s.CanEnter(player))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault() ?? _states[0];

        if (!_candidates.TryGetValue(player.PlayerId, out var cand) || cand.desiredName != desired.Name)
        {
            _candidates[player.PlayerId] = (desired.Name, tick);
            return; // wait to sustain
        }

        var sustained = tick - cand.sinceTick;
        if (sustained < DebounceTicks)
        {
            return;
        }

        // If current differs, transition
        string currentName = player.MovementStateName;

        if (currentName != desired.Name)
        {
            // Exit current
            var current = _states.FirstOrDefault(s => s.Name == currentName);
            current?.OnExit(player, tick);

            // Enter desired
            desired.OnEnter(player, tick);
        }
    }
}
