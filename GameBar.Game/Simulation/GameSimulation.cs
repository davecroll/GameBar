using GameBar.Game.Models;
using GameBar.Game.Simulation.PlayerFsm;

namespace GameBar.Game.Simulation;

public class GameSimulation : IGameSimulation
{
    public GameState State { get; } = new();

    private readonly Dictionary<string, InputCommand> _latestInputs = new();

    private readonly MovementStateMachine _movementFsm = new();
    private readonly ActionStateMachine _actionFsm = new(new[] { new PlayerFsm.Action.JabState(50, 10, 80) });

    public void AddPlayer(string playerId)
    {
        if (!State.Players.ContainsKey(playerId))
        {
            State.Players[playerId] = new PlayerSnapshot
            {
                PlayerId = playerId,
                X = 0,
                Y = 0, // start on ground
                VX = 0,
                VY = 0,
                IsGrounded = true,
                GroundY = 0,
                MovementState = MovementState.Idle,
                LastActivityTick = State.Tick,
                MovementStateName = "Idle",
                MovementStateStartTick = State.Tick
            };
        }
    }

    public void RemovePlayer(string playerId)
    {
        State.Players.Remove(playerId);
        _latestInputs.Remove(playerId);
    }

    public void EnqueueInput(string playerId, InputCommand input) => _latestInputs[playerId] = input;

    public void Update(TimeSpan dt)
    {
        var dtSeconds = (float)dt.TotalSeconds;

        foreach (var (playerId, player) in State.Players.ToArray())
        {
            _latestInputs.TryGetValue(playerId, out var input);

            // Horizontal velocity from input (air control factored)
            player.VX = input is null ? 0 : InputProcessing.ComputeHorizontalVelocity(input, !player.IsGrounded);

            // Jump trigger (edge) when grounded and Jump flag true
            if (input?.Jump == true && player.IsGrounded)
            {
                player.IsGrounded = false;
                player.VY = InputProcessing.JumpVelocity;
            }

            // Apply gravity if airborne
            if (!player.IsGrounded)
            {
                player.VY -= InputProcessing.Gravity * dtSeconds;
            }

            // Integrate positions
            player.X += player.VX * dtSeconds;
            player.Y += player.VY * dtSeconds;

            // Ground collision (simple plane at GroundY)
            if (player.Y <= player.GroundY)
            {
                player.Y = player.GroundY;
                player.VY = 0;
                player.IsGrounded = true;
            }

            _movementFsm.Evaluate(player, State.Tick);
            _actionFsm.Evaluate(player, input, State.Tick);
        }

        State.Tick++;
        State.LastUpdated = DateTimeOffset.UtcNow;
    }
}
