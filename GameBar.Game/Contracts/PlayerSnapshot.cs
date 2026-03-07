namespace GameBar.Game.Contracts;

public class PlayerSnapshot
{
    public string PlayerId { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }
    public bool IsGrounded { get; set; } = true;
    public long LastActivityTick { get; set; }
    public string MovementStateName { get; set; } = string.Empty;
    public long MovementStateStartTick { get; set; }
    public string? ActionStateName { get; set; }
    public long? ActionStateStartTick { get; set; }
}
