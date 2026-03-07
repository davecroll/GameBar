namespace GameBar.Game;

public static class GameConstants
{
    /// <summary>Ground collision Y position in pixels from the top of the screen.</summary>
    public const float GroundY = 400f;

    /// <summary>Server simulation tick rate in Hz (ticks per second).</summary>
    public const int TickRateHz = 40;

    /// <summary>Duration of one simulation tick in milliseconds.</summary>
    public const float TickIntervalMs = 1000f / TickRateHz;

    /// <summary>Duration of one simulation tick in seconds.</summary>
    public const float TickIntervalSeconds = TickIntervalMs / 1000f;
}
