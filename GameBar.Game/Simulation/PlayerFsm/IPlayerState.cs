using GameBar.Game.Models;

namespace GameBar.Game.Simulation.PlayerFsm;

/// <summary>
/// Interface for a player state in a given layer (e.g., Movement).
/// Focused on transition logic; animation metadata can be added later.
/// </summary>
public interface IPlayerState
{
    string Name { get; }
    string Layer { get; } // e.g., "Movement"
    int Priority { get; } // higher value wins

    bool CanEnter(PlayerSnapshot player);
    bool CanContinue(PlayerSnapshot player);

    void OnEnter(PlayerSnapshot player, long tick);
    void OnExit(PlayerSnapshot player, long tick);
}
