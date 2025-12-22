using GameBar.Game.Simulation.PlayerFsm;
using GameBar.Game.Simulation.PlayerFsm.Action;

namespace GameBar.Game.Models;

/// <summary>
/// Authoritative, server-side player model. This is mutable simulation state and may contain
/// runtime-only references (e.g., FSM state instances).
/// 
/// Convert to <see cref="PlayerSnapshot"/> for network transport.
/// </summary>
public sealed class Player
{
    public string PlayerId { get; set; } = string.Empty;

    public float X { get; set; }
    public float Y { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }

    public bool IsGrounded { get; set; } = true;

    public long LastActivityTick { get; set; }

    // Movement/action state identity + tick-based "timers" for determinism.
    public string MovementStateName { get; set; } = string.Empty;
    public long MovementStateStartTick { get; set; }

    public string? ActionStateName { get; set; }
    public long? ActionStateStartTick { get; set; }

    // Runtime-only references (not for transport).
    public IPlayerState? MovementState { get; set; }
    public IActionState? ActionState { get; set; }

    public BoundingBox? Hurtbox(long currentTick)
    {
        var relativeBoundingBox = MovementState?.Hurtbox(this, currentTick);
        if (relativeBoundingBox is null) return null;
        return new BoundingBox(
            (int)(X + relativeBoundingBox.Value.X),
            (int)(Y + relativeBoundingBox.Value.Y),
            relativeBoundingBox.Value.Width,
            relativeBoundingBox.Value.Height);
    }

    public BoundingBox? Hitbox()
    {
        var relativeBoundingBox = ActionState?.Hitbox;
        if (relativeBoundingBox is null) return null;
        return new BoundingBox(
            (int)(X + relativeBoundingBox.Value.X),
            (int)(Y + relativeBoundingBox.Value.Y),
            relativeBoundingBox.Value.Width,
            relativeBoundingBox.Value.Height);
    }

    public PlayerSnapshot ToSnapshot()
    {
        return new PlayerSnapshot
        {
            PlayerId = PlayerId,
            X = X,
            Y = Y,
            VX = VX,
            VY = VY,
            IsGrounded = IsGrounded,
            LastActivityTick = LastActivityTick,
            MovementStateName = MovementStateName,
            MovementStateStartTick = MovementStateStartTick,
            ActionStateName = ActionStateName,
            ActionStateStartTick = ActionStateStartTick
        };
    }
}