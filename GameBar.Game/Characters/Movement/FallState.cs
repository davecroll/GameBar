using GameBar.Game.Models;
using GameBar.Game.StateMachine;

namespace GameBar.Game.Characters.Movement;

public sealed class FallState : IMovementState
{
    public string Name => "Fall";
    public string Layer => "Movement";
    public int Priority => 15; // between run and jump

    public bool CanEnter(Player player) => !player.IsGrounded && player.VY >= 0.0f;
    public bool CanContinue(Player player) => !player.IsGrounded && player.VY >= 0.0f;

    public void OnEnter(Player player, long tick)
    {
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(Player player, long tick) { }
}
