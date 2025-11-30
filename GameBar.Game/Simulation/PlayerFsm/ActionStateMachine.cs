using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm;

public sealed class ActionStateMachine
{
    private const int DebounceTicks = 1;
    private readonly Dictionary<string, (string desiredName, long sinceTick)> _candidates = new();

    // For now, hardcode Jab duration in ticks; later fetch from manifest if needed on server
    private readonly int _jabDurationTicks;

    public ActionStateMachine(int tickDurationMs, int jabFrameCount, int jabFrameDurationMs)
    {
        _jabDurationTicks = (int)Math.Max(1, (long)jabFrameCount * jabFrameDurationMs / tickDurationMs);
    }

    public void Evaluate(PlayerState player, InputCommand? input, long tick)
    {
        // If action active and finished, clear it
        if (!string.IsNullOrEmpty(player.ActionStateName))
        {
            var start = player.ActionStateStartTick ?? tick;
            var elapsed = tick - start;
            if (elapsed >= _jabDurationTicks)
            {
                player.ActionStateName = null;
                player.ActionStateStartTick = null;
            }
            return;
        }

        // Trigger Jab if Attack pressed
        if (input is not null && input.Attack)
        {
            // Debounce and set desired
            if (!_candidates.TryGetValue(player.PlayerId, out var cand) || cand.desiredName != "Jab")
            {
                _candidates[player.PlayerId] = ("Jab", tick);
                return;
            }
            var sustained = tick - cand.sinceTick;
            if (sustained >= DebounceTicks)
            {
                player.ActionStateName = "Jab";
                player.ActionStateStartTick = tick;
            }
        }
    }
}

