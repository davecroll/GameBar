namespace GameBar.Game.Models;

public readonly struct FrameData
{
    // has to be Lazy to avoid "cycle" in struct definition
    private readonly Lazy<FrameData> _invertedFrameData;

    public FrameData(int index,
        PixelSize size,
        IDictionary<string, BoundingBox> collisionBoxes,
        BoundingBox? hitbox,
        float duration)
    {
        Index = index;
        Size = size;
        CollisionBoxes = collisionBoxes;
        Hitbox = hitbox;
        Duration = duration;

        _invertedFrameData = new Lazy<FrameData>(Invert);
    }

    public int Index { get; }
    public PixelSize Size { get; }
    public IDictionary<string, BoundingBox> CollisionBoxes { get; }
    public BoundingBox? Hitbox { get; }
    public float Duration { get; }
    public FrameData Inverted => _invertedFrameData.Value;

    private FrameData Invert()
    {
        // Dictionary to store inverted collision boxes
        var invertedCollisionBoxes = new Dictionary<string, BoundingBox>();

        // Invert all collision boxes
        foreach (var kvp in CollisionBoxes)
        {
            var key = kvp.Key;
            var box = kvp.Value;

            var invertedBox = new BoundingBox(
                Size.Width - box.X - box.Width,
                box.Y,
                box.Width,
                box.Height
            );

            invertedCollisionBoxes[key] = invertedBox;
        }

        // Invert the hitbox if present
        BoundingBox? invertedHitbox = null;
        if (Hitbox is { } hitbox)
        {
            invertedHitbox = new BoundingBox(
                Size.Width - hitbox.X - hitbox.Width,
                hitbox.Y,
                hitbox.Width,
                hitbox.Height
            );
        }

        // Return a new FrameData instance with inverted collision boxes and hitbox
        return new FrameData(
            Index,
            Size,
            invertedCollisionBoxes,
            invertedHitbox,
            Duration
        );
    }
}
