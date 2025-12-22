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

    BoundingBox? Hurtbox(Player player, long currentTick) => null;

    bool CanEnter(Player player);
    bool CanContinue(Player player);

    void OnEnter(Player player, long tick);
    void OnExit(Player player, long tick);
}
