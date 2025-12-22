using GameBar.Game.Models;

namespace GameBar.Game.Simulation;

/// <summary>
/// Shared input-to-velocity processing for deterministic movement across client and server.
/// </summary>
public static class InputProcessing
{
    public const float HorizontalSpeed = 150f; // adjust for feel
    public const float JumpVelocity = 500f;    // upward initial velocity
    public const float Gravity = 1200f;        // downward acceleration
    public const float AirControlFactor = 0.9f; // horizontal control multiplier while airborne (optional)

    public static float ComputeHorizontalVelocity(InputCommand input, bool airborne)
    {
        float dx = 0;
        if (input.Left) dx -= 1;
        if (input.Right) dx += 1;
        if (dx != 0)
        {
            dx = dx > 0 ? 1 : -1; // no diagonal; simple left/right
        }
        var speed = HorizontalSpeed * (airborne ? AirControlFactor : 1f);
        return dx * speed;
    }
}
