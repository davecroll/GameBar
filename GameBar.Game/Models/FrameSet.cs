namespace GameBar.Game.Models;

public class FrameSet
{
    private readonly float[] _frameBreaks;
    private readonly List<FrameData> _frames;

    public FrameSet(List<FrameData> frames)
    {
        _frames = frames;
        _frameBreaks = new float[frames.Count];

        // Precompute frame breakpoints based on frame durations.
        float runningTotal = 0;
        foreach (var (index, frame) in frames.Index())
        {
            _frameBreaks[index] = runningTotal;
            runningTotal += frame.Duration;
        }

        Duration = runningTotal;
    }

    public float Duration { get; }

    public FrameData this[int index] => _frames[index];

    public FrameData GetFrameData(float elapsedSeconds)
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
        return _frames[frameIndex];
    }
}