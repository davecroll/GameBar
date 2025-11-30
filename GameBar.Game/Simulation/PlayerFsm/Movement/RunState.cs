using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public sealed class RunState : PlayerFsm.IPlayerState
{
    public string Name => "Run";
    public string Layer => "Movement";
    public int Priority => 20; // higher than Idle

    public bool CanEnter(PlayerState player)
    {
        return Math.Abs(player.VX) >= 0.0001f || Math.Abs(player.VY) >= 0.0001f;
    }

    public bool CanContinue(PlayerState player)
    {
        return CanEnter(player);
    }

    public void OnEnter(PlayerState player, long tick)
    {
        player.MovementState = MovementState.Running;
        player.RunningStartTick = tick;
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(PlayerState player, long tick)
    {
        // no-op
    }
}
