using GameBar.Game.Models;
using GameBar.Game.Simulation.StateMachine;

namespace GameBar.Game.Simulation.Characters.Movement;

public sealed class JumpState : IMovementState
{
    public string Name => "Jump";
    public string Layer => "Movement";
    public int Priority => 20; // higher than run/idle

    public bool CanEnter(Player player) => !player.IsGrounded && player.VY < 0.0f;
    public bool CanContinue(Player player) => !player.IsGrounded && player.VY < 0.0f;

    public void OnEnter(Player player, long tick)
    {
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(Player player, long tick) { }
}
