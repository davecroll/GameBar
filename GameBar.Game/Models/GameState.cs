namespace GameBar.Game.Models;

public class GameState
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public Dictionary<string, PlayerState> Players { get; set; } = new();
    public long Tick { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}
