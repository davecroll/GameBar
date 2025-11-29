using GameBar.Game.Models;

namespace GameBar.Game.Simulation;

/// <summary>
/// Shared input-to-velocity processing for deterministic movement across client and server.
/// </summary>
public static class InputProcessing
{
    public const float Speed = 25f; // units per second

    /// <summary>
    /// Converts directional input flags into a normalized velocity vector scaled by Speed.
    /// </summary>
    public static (float vx, float vy) InputToVelocity(InputCommand input)
    {
        float dx = 0, dy = 0;
        if (input.Up) dy -= 1;
        if (input.Down) dy += 1;
        if (input.Left) dx -= 1;
        if (input.Right) dx += 1;

        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length > 0)
        {
            dx /= length;
            dy /= length;
        }

        return (dx * Speed, dy * Speed);
    }
}

