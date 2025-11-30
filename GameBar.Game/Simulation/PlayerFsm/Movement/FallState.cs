using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public sealed class FallState : PlayerFsm.IPlayerState
{
    public string Name => "Fall";
    public string Layer => "Movement";
    public int Priority => 15; // between run and jump

    public bool CanEnter(PlayerSnapshot player)
    {
        // Enter when not grounded and vertical velocity <= 0 (descending or apex)
        return !player.IsGrounded && player.VY <= 0.0f;
    }

    public bool CanContinue(PlayerSnapshot player)
    {
        return !player.IsGrounded && player.VY <= 0.0f;
    }

    public void OnEnter(PlayerSnapshot player, long tick)
    {
        player.MovementState = MovementState.Fall;
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(PlayerSnapshot player, long tick) { }
}
