namespace GameBar.Game.Models;

public class FrameSet
{
    private readonly float[] _frameBreaks;
    private readonly List<FrameData> _frames;

    public FrameSet(List<FrameData> frames)
    {
        _frames = frames;

        float runningTotal = 0;
        _frameBreaks = frames.Select(f =>
        {
            float breakpoint = runningTotal;
            runningTotal += f.Duration;
            return breakpoint;
        }).ToArray();

        Duration = runningTotal;
    }

    public float Duration { get; }

    public FrameData this[int index] => _frames[index];

    public FrameData GetFrameData(float elapsedSeconds, bool invert = false)
    {
        // Calculate the normalized time within the animation cycle.
        float normalizedTime = elapsedSeconds % Duration;

        // Find the corresponding frame index.
        int frameIndex = Array.FindLastIndex(_frameBreaks, mark => normalizedTime >= mark);

        // If the index is invalid (e.g., -1), fallback to the last frame as a safeguard.
        if (frameIndex == -1)
        {
            Console.WriteLine("WARNING: Invalid frame index. Falling back to last frame.");
            frameIndex = _frames.Count - 1;
        }

        // Return the frame data.
        FrameData frameData = _frames[frameIndex];

        return invert ? frameData.Inverted : frameData;
    }
}
