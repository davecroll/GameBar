using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Action;

public sealed class JabState : IActionState
{
    public string Name => "Jab";
    public string Layer => "Action";
    public int Priority => 10; // basic attack

    public int DurationTicks { get; }
    public bool Interruptible => false; // simple non-interruptible jab

    private readonly int _tickDurationMs;
    private readonly int _frameCount;
    private readonly int _frameDurationMs;

    public JabState(int tickDurationMs = 50, int frameCount = 10, int frameDurationMs = 80)
    {
        _tickDurationMs = tickDurationMs;
        _frameCount = frameCount;
        _frameDurationMs = frameDurationMs;
        DurationTicks = Math.Max(1, (frameCount * frameDurationMs) / tickDurationMs);
    }

    public bool CanEnter(PlayerSnapshot player)
    {
        return true;
    }

    public bool CanContinue(PlayerSnapshot player)
    {
        return true;
    }

    public bool CanTrigger(PlayerSnapshot player, InputCommand? input)
    {
        return input is not null && input.Attack;
    }

    public void OnEnter(PlayerSnapshot player, long tick)
    {
        player.ActionStateName = Name;
        player.ActionStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(PlayerSnapshot player, long tick)
    {
        player.ActionStateName = null;
        player.ActionStateStartTick = null;
    }
}
