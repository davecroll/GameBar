using GameBar.Game.Models;

namespace GameBar.Game.Simulation;

/// <summary>
/// Shared input-to-velocity processing for deterministic movement across client and server.
/// </summary>
public static class InputProcessing
{
    public const float HorizontalSpeed = 75f; // horizontal units per second
    public const float JumpVelocity = 300f; // initial vertical velocity for jump
    public const float Gravity = 800f; // gravity acceleration (units/sec^2)

    /// <summary>
    /// Converts directional input flags into a normalized velocity vector scaled by HorizontalSpeed.
    /// </summary>
    public static (float vx, float vy) InputToHorizontalVelocity(InputCommand input)
    {
        float dx = 0, dy = 0;
        if (input.Left) dx -= 1;
        if (input.Right) dx += 1;
        // Up/Down ignored for platformer horizontal plane (could be ladder/climb later)

        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length > 0)
        {
            dx /= length;
            dy /= length;
        }

        return (dx * HorizontalSpeed, dy * HorizontalSpeed);
    }
}
