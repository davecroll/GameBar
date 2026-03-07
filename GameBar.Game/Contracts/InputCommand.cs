namespace GameBar.Game.Contracts;

public class InputCommand
{
    public string PlayerId { get; set; } = string.Empty;
    public long ClientInputSequence { get; set; }
    public long ClientTick { get; set; }
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Attack { get; set; }
    public bool Jump { get; set; }
}
