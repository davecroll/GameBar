namespace GameBar.Game.Models;

public class StateSnapshot
{
    public Guid GameId { get; set; }
    public long ServerTick { get; set; }
    public Dictionary<string, PlayerState> Players { get; set; } = new();
    public Dictionary<string, long> LastProcessedInputSequenceByPlayer { get; set; } = new();
}

