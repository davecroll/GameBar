namespace GameBar.Game.Models;

public enum MovementState
{
    Unknown = 0,
    Idle = 1,
    Running = 2
}

public class PlayerSnapshot
{
    public string PlayerId { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }

    // Movement/animation state tracked by server ticks
    public MovementState MovementState { get; set; } = MovementState.Unknown;
    public long LastActivityTick { get; set; }

    // Data-driven layered states
    public string MovementStateName { get; set; } = string.Empty;
    public long MovementStateStartTick { get; set; }
    public string? ActionStateName { get; set; }
    public long? ActionStateStartTick { get; set; }
}
