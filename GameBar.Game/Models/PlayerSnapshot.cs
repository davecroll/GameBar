using System.Text.Json.Serialization;

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
    public string PlayerId { get; set; } = string.Empty;
    public float X { get; set; } // horizontal position (left/right)
    public float Y { get; set; } // vertical position (up/down); ground at GroundY
    public float VX { get; set; } // horizontal velocity
    public float VY { get; set; } // vertical velocity

    public bool IsGrounded { get; set; } = true; // grounded flag for jump/fall logic

    public long LastActivityTick { get; set; }
    public string MovementStateName { get; set; } = string.Empty;
    public long MovementStateStartTick { get; set; }
    public string? ActionStateName { get; set; }
    public long? ActionStateStartTick { get; set; }

    // This type is intended for transport/serialization.
    // Keep it reference-free and deterministic.
    [JsonIgnore]
    public bool HasActiveAction => !string.IsNullOrEmpty(ActionStateName) && ActionStateStartTick is not null;
}