using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm;

/// <summary>
/// Movement-layer state contract.
/// </summary>
public interface IMovementState : IPlayerState
{
    bool CanEnter(Player player);

    /// <summary>
    /// Whether the current movement state should be allowed to continue.
    /// Not currently used by <see cref="MovementStateMachine"/>, but kept for future
    /// "sticky" movement states (e.g., landing, knockback) and symmetry with existing implementations.
    /// </summary>
    bool CanContinue(Player player);
}

