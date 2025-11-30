using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public sealed class JumpState : PlayerFsm.IPlayerState
{
    public string Name => "Jump";
    public string Layer => "Movement";
    public int Priority => 20; // higher than run/idle

    public bool CanEnter(PlayerSnapshot player)
    {
        // Enter when upward vertical velocity > 0 and not grounded
        return !player.IsGrounded && player.VY > 0.0f;
    }

    public bool CanContinue(PlayerSnapshot player)
    {
        // Continue until vertical velocity goes negative (start falling)
        return !player.IsGrounded && player.VY > 0.0f;
    }

    public void OnEnter(PlayerSnapshot player, long tick)
    {
        player.MovementState = MovementState.Jump;
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(PlayerSnapshot player, long tick) { }
}
