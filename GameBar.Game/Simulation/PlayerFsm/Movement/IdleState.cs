using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public sealed class IdleState : IMovementState
{
    private readonly FrameSet _frameSet = new IdleFrameSet();

    public string Name => "Idle";
    public string Layer => "Movement";
    public int Priority => 5;

    public bool CanEnter(Player player) => player.IsGrounded && Math.Abs(player.VX) < 0.0001f;
    public bool CanContinue(Player player) => player.IsGrounded && Math.Abs(player.VX) < 0.0001f;

    public void OnEnter(Player player, long tick)
    {
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(Player player, long tick) { }

    public BoundingBox? Hurtbox(Player player, long currentTick)
    {
        var ticksSinceStart = currentTick - player.MovementStateStartTick;
        var elapsedSeconds = ticksSinceStart * 0.05f; // 50ms fixed timestep
        var frameData = _frameSet.GetFrameData(elapsedSeconds);
        return frameData.CollisionBoxes.FirstOrDefault().Value;
    }
}
