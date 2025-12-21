using System.Text.Json.Serialization;
using GameBar.Game.Simulation.PlayerFsm;
using GameBar.Game.Simulation.PlayerFsm.Action;

namespace GameBar.Game.Models;

public enum MovementState
{
    Unknown = 0,
    Idle = 1,
    Running = 2,
    Jump = 3,
    Fall = 4
}

public class PlayerSnapshot
{
    // ...existing code...
    public string PlayerId { get; set; } = string.Empty;
    public float X { get; set; } // horizontal position (left/right)
    public float Y { get; set; } // vertical position (up/down); ground at GroundY
    public float VX { get; set; } // horizontal velocity
    public float VY { get; set; } // vertical velocity

    public bool IsGrounded { get; set; } = true; // grounded flag for jump/fall logic

    // ...existing code...
    public long LastActivityTick { get; set; }
    public string MovementStateName { get; set; } = string.Empty;
    public long MovementStateStartTick { get; set; }
    public string? ActionStateName { get; set; }
    public long? ActionStateStartTick { get; set; }

    [JsonIgnore]
    public IPlayerState? MovementState { get; set; }

    [JsonIgnore]
    public IActionState? ActionState { get; set; }

    public BoundingBox? Hurtbox()
    {
        var relativeBoundingBox = MovementState?.Hurtbox;
        if (relativeBoundingBox is not null)
        {
            return new BoundingBox(
                (int)(X + relativeBoundingBox.Value.X),
                (int)(Y + relativeBoundingBox.Value.Y),
                relativeBoundingBox.Value.Width,
                relativeBoundingBox.Value.Height);
        }

        return null;
    }

    public BoundingBox? Hitbox()
    {
        var relativeBoundingBox = ActionState?.Hitbox;
        if (relativeBoundingBox is not null)
        {
            return new BoundingBox(
                (int)(X + relativeBoundingBox.Value.X),
                (int)(Y + relativeBoundingBox.Value.Y),
                relativeBoundingBox.Value.Width,
                relativeBoundingBox.Value.Height);
        }

        return null;
    }
}