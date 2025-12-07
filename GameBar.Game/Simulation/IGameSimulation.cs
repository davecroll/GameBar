using GameBar.Game.Models;

namespace GameBar.Game.Simulation;

/// <summary>
/// Core game simulation contract used by both server and client.
/// </summary>
/// <remarks>
/// Consumers are expected to:
/// <list type="bullet">
/// <item>
/// <description>Create one instance per match / game session.</description>
/// </item>
/// <item>
/// <description>Call <see cref="Update"/> at a fixed time step (e.g. 50 ms) from a host-driven loop.</description>
/// </item>
/// <item>
/// <description>Treat <see cref="State"/> as owned by the simulation (do not mutate externally).</description>
/// </item>
/// </list>
/// </remarks>
public interface IGameSimulation
{
    /// <summary>
    /// The live, authoritative game state. This object is mutated only by <see cref="Update"/>.
    /// Callers should treat it as read-only and avoid external mutation.
    /// </summary>
    GameState State { get; }

    /// <summary>
    /// Adds a new player to the simulation. No-op if the player already exists.
    /// </summary>
    /// <param name="playerId">Stable, unique identifier for the player (e.g. connection id).</param>
    void AddPlayer(string playerId);

    /// <summary>
    /// Removes a player and any associated input from the simulation.
    /// </summary>
    /// <param name="playerId">Identifier previously passed to <see cref="AddPlayer"/>.</param>
    void RemovePlayer(string playerId);

    /// <summary>
    /// Queues or records the latest input for the given player. Implementations are expected to
    /// apply this input on the next <see cref="Update"/> call and may overwrite any previous
    /// unprocessed input for the same player.
    /// </summary>
    /// <param name="playerId">Player identifier.</param>
    /// <param name="input">The most recent input snapshot from the client.</param>
    void EnqueueInput(string playerId, InputCommand input);

    /// <summary>
    /// Advances the simulation by the given delta time. Typical hosts call this with a fixed
    /// time step (e.g. 50 ms) to keep the simulation deterministic and frame-rate independent.
    /// Implementations should update <see cref="GameState.Tick"/> and <see cref="GameState.LastUpdated"/>
    /// as part of this call.
    /// </summary>
    /// <param name="dt">Time step to advance the simulation by.</param>
    void Update(TimeSpan dt);
}
