namespace GameBar.Game.Models;

public struct BoundingBox(int x, int y, int width, int height)
{
    public int X = x;
    public int Y = y;
    public int Width = width;
    public int Height = height;

    public int Left => X;
    public int Right => X + Width;
    public int Top => Y;
    public int Bottom => Y + Height;

    public bool Intersects(BoundingBox value) =>
        value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;

    // public static BoundingBox operator +(BoundingBox box, PixelPosition position) =>
    //     new(box.X + position.X, box.Y + position.Y, box.Width, box.Height);

    public override string ToString() => $"[X={X}, Y={Y}, W={Width}, H={Height}]";
}
