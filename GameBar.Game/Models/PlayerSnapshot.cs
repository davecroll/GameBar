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
    public float X { get; set; }
    public float Y { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }
    public float Z { get; set; } // vertical position above ground (0 = on ground)
    public float VZ { get; set; } // vertical velocity
    public bool IsGrounded { get; set; } = true; // grounded flag for jump/fall logic
    public float GroundY { get; set; } = 0f; // baseline ground height (expandable to platforms later)
    // ...existing code...
    public MovementState MovementState { get; set; } = MovementState.Unknown;
    public long LastActivityTick { get; set; }
    public string MovementStateName { get; set; } = string.Empty;
    public long MovementStateStartTick { get; set; }
    public string? ActionStateName { get; set; }
    public long? ActionStateStartTick { get; set; }
}

