using GameBar.Game.Models;
using GameBar.Game.Simulation.PlayerFsm.Action;

namespace GameBar.Game.Simulation.PlayerFsm;

public sealed class ActionStateMachine
{
    private const int DebounceTicks = 1;
    private readonly Dictionary<string, (string desiredName, long sinceTick)> _candidates = new();
    private readonly List<IActionState> _states;

    public ActionStateMachine(IEnumerable<IActionState>? states = null)
    {
        _states = (states?.ToList() ?? new List<IActionState> { new JabState() })
            .OrderByDescending(s => s.Priority)
            .ToList();
    }

    public void Evaluate(PlayerSnapshot player, InputCommand? input, long tick)
    {
        // If action active, check duration completion
        if (!string.IsNullOrEmpty(player.ActionStateName))
        {
            var current = _states.FirstOrDefault(s => s.Name == player.ActionStateName);
            if (current is null)
            {
                // Unknown state, clear
                player.ActionStateName = null;
                player.ActionStateStartTick = null;
                return;
            }

            var start = player.ActionStateStartTick ?? tick;
            var elapsed = tick - start;
            if (elapsed >= current.DurationTicks)
            {
                current.OnExit(player, tick);
            }
            return;
        }

        // No active action: see if any state wants to trigger based on input
        var desired = _states.FirstOrDefault(s => s.CanTrigger(player, input));
        if (desired is null)
        {
            return;
        }

        if (!_candidates.TryGetValue(player.PlayerId, out var cand) || cand.desiredName != desired.Name)
        {
            _candidates[player.PlayerId] = (desired.Name, tick);
            return;
        }

        var sustained = tick - cand.sinceTick;
        if (sustained >= DebounceTicks)
        {
            desired.OnEnter(player, tick);
        }
    }
}
