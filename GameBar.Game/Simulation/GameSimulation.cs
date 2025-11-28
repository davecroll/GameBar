using GameBar.Game.Models;

namespace GameBar.Game.Simulation;

public class GameSimulation : IGameSimulation
{
    public GameState State { get; } = new();

    private readonly Dictionary<string, InputCommand> _latestInputs = new();

    private const float Speed = 5f; // units per second

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
                (player.VX, player.VY) = InputToVelocity(input);
            }

            player.X += player.VX * dtSeconds;
            player.Y += player.VY * dtSeconds;
        }

        State.Tick++;
        State.LastUpdated = DateTimeOffset.UtcNow;
    }

    private static (float vx, float vy) InputToVelocity(InputCommand input)
    {
        float dx = 0, dy = 0;
        if (input.Up) dy -= 1;
        if (input.Down) dy += 1;
        if (input.Left) dx -= 1;
        if (input.Right) dx += 1;

        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length > 0)
        {
            dx /= length;
            dy /= length;
        }

        return (dx * Speed, dy * Speed);
    }
}

