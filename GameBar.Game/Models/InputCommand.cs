namespace GameBar.Game.Models;

public class InputCommand
{
    public string PlayerId { get; set; } = string.Empty;
    public long ClientInputSequence { get; set; }
    public long ClientTick { get; set; }
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
}

