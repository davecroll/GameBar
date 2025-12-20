using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm.Movement;

public sealed class RunState : IPlayerState
{
    public string Name => "Run";
    public string Layer => "Movement";
    public int Priority => 10;

    public bool CanEnter(PlayerSnapshot player)
    {
        return player.IsGrounded && Math.Abs(player.VX) >= 0.0001f;
    }

    public bool CanContinue(PlayerSnapshot player) => CanEnter(player);

    public void OnEnter(PlayerSnapshot player, long tick)
    {
        player.MovementStateName = Name;
        player.MovementStateStartTick = tick;
        player.LastActivityTick = tick;
    }

    public void OnExit(PlayerSnapshot player, long tick) { }
}
