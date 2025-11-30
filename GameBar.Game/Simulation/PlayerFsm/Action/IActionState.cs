using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Action;

/// <summary>
/// Interface for action-layer states (e.g., attacks) with trigger semantics and duration.
/// </summary>
public interface IActionState : IPlayerState
{
    /// <summary>
    /// Whether this action should trigger given current input. Called when no action is active.
    /// </summary>
    bool CanTrigger(PlayerSnapshot player, InputCommand? input);

    /// <summary>
    /// Duration of the action in ticks. Non-looping action ends when elapsed ticks exceed this value.
    /// </summary>
    int DurationTicks { get; }

    /// <summary>
    /// Whether this action can be interrupted by another higher-priority action.
    /// </summary>
    bool Interruptible { get; }
}
