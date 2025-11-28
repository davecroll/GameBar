using GameBar.Game.Models;

namespace GameBar.Game.Simulation;

public interface IGameSimulation
{
    GameState State { get; }
    void AddPlayer(string playerId);
    void RemovePlayer(string playerId);
    void EnqueueInput(string playerId, InputCommand input);
    void Update(TimeSpan dt);
}

