using GameBar.Game.Models;
using GameBar.Game.Simulation;

namespace GameBar.Game.Simulation;

public class GameSimulation : IGameSimulation
{
    public GameState State { get; } = new();

    private readonly Dictionary<string, InputCommand> _latestInputs = new();

    public void AddPlayer(string playerId)
    {
        if (!State.Players.ContainsKey(playerId))
        {
            State.Players[playerId] = new PlayerState
            {
                PlayerId = playerId,
                X = 0,
                Y = 0,
                VX = 0,
                VY = 0
            };
        }
    }

    public void RemovePlayer(string playerId)
    {
        State.Players.Remove(playerId);
        _latestInputs.Remove(playerId);
    }

    public void EnqueueInput(string playerId, InputCommand input)
    {
        _latestInputs[playerId] = input;
    }

    public void Update(TimeSpan dt)
    {
        var dtSeconds = (float)dt.TotalSeconds;

        foreach (var (playerId, player) in State.Players.ToArray())
        {
            if (!_latestInputs.TryGetValue(playerId, out var input))
            {
                player.VX = 0;
                player.VY = 0;
            }
            else
            {
                (player.VX, player.VY) = InputProcessing.InputToVelocity(input);
            }

            player.X += player.VX * dtSeconds;
            player.Y += player.VY * dtSeconds;
        }

        State.Tick++;
        State.LastUpdated = DateTimeOffset.UtcNow;
    }
}
