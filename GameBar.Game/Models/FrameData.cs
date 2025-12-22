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
        var frameData = this;
        var invertedCollisionBoxes = CollisionBoxes
            .ToDictionary(kvp => kvp.Key, kvp => frameData.InvertBox(kvp.Value));

        var invertedHitbox = Hitbox is { } hitbox ? InvertBox(hitbox) : (BoundingBox?)null;

        return new FrameData(Index, Size, invertedCollisionBoxes, invertedHitbox, Duration);
    }

    private BoundingBox InvertBox(BoundingBox box)
        => new(Size.Width - box.X - box.Width, box.Y, box.Width, box.Height);
}