using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public class IdleFrameSet() : FrameSet(Frames)
{
    private static readonly List<FrameData> Frames =
    [
        new(
            0,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 11, 8, 8) }, { "Body", new BoundingBox(16, 20, 14, 20) }
            },
            null,
            0.095f),
        new(
            1,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 13, 8, 8) }, { "Body", new BoundingBox(16, 22, 14, 18) }
            },
            null,
            0.095f),
        new(
            2,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 14, 8, 8) }, { "Body", new BoundingBox(16, 22, 14, 18) }
            },
            null,
            0.095f),
        new(
            3,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 14, 8, 8) }, { "Body", new BoundingBox(16, 22, 14, 18) }
            },
            null,
            0.095f),
        new(
            4,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 14, 8, 8) }, { "Body", new BoundingBox(16, 22, 14, 18) }
            },
            null,
            0.095f),
        new(
            5,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 13, 8, 8) }, { "Body", new BoundingBox(16, 22, 14, 18) }
            },
            null,
            0.095f),
        new(
            6,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 11, 8, 8) }, { "Body", new BoundingBox(16, 20, 14, 20) }
            },
            null,
            0.095f),
        new(
            7,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 10, 8, 8) }, { "Body", new BoundingBox(16, 19, 14, 21) }
            },
            null,
            0.095f),
        new(
            8,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 10, 8, 8) }, { "Body", new BoundingBox(16, 19, 14, 21) }
            },
            null,
            0.095f),
        new(
            9,
            new PixelSize(48, 48),
            new Dictionary<string, BoundingBox>
            {
                { "Head", new BoundingBox(21, 10, 8, 8) }, { "Body", new BoundingBox(16, 19, 14, 21) }
            },
            null,
            0.095f)
    ];
}