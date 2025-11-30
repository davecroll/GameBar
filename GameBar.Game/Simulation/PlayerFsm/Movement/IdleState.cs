using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public sealed class IdleState : PlayerFsm.IPlayerState
{
    public string Name => "Idle";
    public string Layer => "Movement";
    public int Priority => 5;

    public bool CanEnter(PlayerSnapshot player)
    {
        return player.IsGrounded && Math.Abs(player.VX) < 0.0001f;
    }

    public bool CanContinue(PlayerSnapshot player) => CanEnter(player);

    public void OnEnter(PlayerSnapshot player, long tick)
    {
        player.MovementState = MovementState.Idle;
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(PlayerSnapshot player, long tick) { }
}
