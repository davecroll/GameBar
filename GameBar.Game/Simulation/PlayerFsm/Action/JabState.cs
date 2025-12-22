using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Action;

public sealed class JabState : IActionState
{
    public string Name => "Jab";
    public string Layer => "Action";
    public int Priority => 10; // basic attack

    public int DurationTicks { get; }
    public bool Interruptible => false; // simple non-interruptible jab

    public BoundingBox? Hitbox => new BoundingBox(0, 0, 48, 48);

    public JabState(int tickDurationMs = 50, int frameCount = 10, int frameDurationMs = 80)
    {
        DurationTicks = Math.Max(1, (frameCount * frameDurationMs) / tickDurationMs);
    }


    public bool CanTrigger(Player player, InputCommand? input)
    {
        return input is not null && input.Attack;
    }

    public void OnEnter(Player player, long tick)
    {
        player.ActionStateName = Name;
        player.ActionStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(Player player, long tick)
    {
        player.ActionStateName = null;
        player.ActionStateStartTick = null;
    }
}