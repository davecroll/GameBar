using GameBar.Game.Models;

namespace GameBar.Game.StateMachine;

/// <summary>
/// Base contract for a state that lives in a specific player FSM layer (e.g., Movement, Action).
/// Contains identity/priority and lifecycle hooks. Layer-specific transition logic belongs in
/// layer-specific interfaces (e.g., <see cref="IMovementState"/>, <see cref="IActionState"/>).
/// </summary>
public interface IPlayerState
{
    string Name { get; }
    string Layer { get; } // e.g., "Movement", "Action"
    int Priority { get; } // higher value wins

    BoundingBox? Hurtbox(Player player, long currentTick) => null;

    void OnEnter(Player player, long tick);
    void OnExit(Player player, long tick);
}
