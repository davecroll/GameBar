namespace GameBar.Game.Models;

public readonly struct PixelSize(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
