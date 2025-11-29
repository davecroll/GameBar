namespace GameBar.Game.Models;

public enum MovementState
{
    Unknown = 0,
    Idle = 1,
    Running = 2
}

public class PlayerState
{
    public string PlayerId { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }

    // Movement/animation state tracked by server ticks
    public MovementState MovementState { get; set; } = MovementState.Unknown;
    public long IdleStartTick { get; set; }
    public long RunningStartTick { get; set; }
    public long LastActivityTick { get; set; }
}
