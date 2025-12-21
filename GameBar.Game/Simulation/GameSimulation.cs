using GameBar.Game.Models;
using GameBar.Game.Simulation.PlayerFsm;
using GameBar.Game.Simulation.PlayerFsm.Action;

namespace GameBar.Game.Simulation;

public class GameSimulation : IGameSimulation
{
    public GameState State { get; } = new();

    private readonly Dictionary<string, InputCommand> _latestInputs = new();

    private readonly MovementStateMachine _movementFsm;
    private readonly ActionStateMachine _actionFsm;

    private const float GroundY = 400;

    public GameSimulation(Dictionary<string, IPlayerState> playerStates)
    {
        // new List<IActionState> { new IdleState(), new RunState(), new FallState(), new JumpState() }
        var movementStates = playerStates.Values.Where(ps => ps.Layer == "Movement").ToList();
        _movementFsm = new MovementStateMachine(movementStates);

        // new List<IActionState> { new JabState() }
        var actionStates = playerStates.Values.Where(ps => ps.Layer == "Action")
            .OfType<IActionState>()
            .ToList();
        _actionFsm = new ActionStateMachine(actionStates);
    }

    public void AddPlayer(string playerId)
    {
        if (State.Players.ContainsKey(playerId)) return;

        State.Players[playerId] = new PlayerSnapshot
        {
            PlayerId = playerId,
            X = 25,
            Y = 100,
            VX = 0,
            VY = 0,
            IsGrounded = false,
            LastActivityTick = State.Tick,
            MovementStateName = "Idle",
            MovementStateStartTick = State.Tick
        };
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

            // Jump trigger when grounded
            if (input?.Jump == true && player.IsGrounded)
            {
                player.IsGrounded = false;
                player.VY = -InputProcessing.JumpVelocity; // negative is up in top-left origin
            }

            // Gravity pulls down (positive Y direction)
            if (!player.IsGrounded)
            {
                player.VY += InputProcessing.Gravity * dtSeconds;
            }

            // Integrate positions
            player.X += player.VX * dtSeconds;
            player.Y += player.VY * dtSeconds;

            // Ground collision at GroundY
            if (player.Y >= GroundY)
            {
                player.Y = GroundY;
                player.VY = 0;
                player.IsGrounded = true;
            }

            _movementFsm.Evaluate(player, State.Tick);
            _actionFsm.Evaluate(player, input, State.Tick);
        }

        DetectDamage();

        State.Tick++;
        State.LastUpdated = DateTimeOffset.UtcNow;
    }

    public void DetectDamage()
    {
        Dictionary<string, BoundingBox?> hurtboxes = new();
        foreach (var (playerId, player) in State.Players.ToArray())
        {
            // get the hurtbox for this player, add it to the hurtbox list
            hurtboxes.Add(playerId, player.Hurtbox());
        }

        Dictionary<string, BoundingBox?> hitboxes = new();
        foreach (var (playerId, player) in State.Players.ToArray())
        {
            // get the hitboxes for this player, add it to the hitboxes list
            hitboxes.Add(playerId, player.Hitbox());
        }

        // check each hitbox against each hurtbox
        foreach (var (attackerId, hitbox) in hitboxes)
        {
            if (hitbox is null) continue;
            foreach (var (defenderId, hurtbox) in hurtboxes)
            {
                if (attackerId == defenderId) continue; // skip self

                if (hurtbox is null) continue;

                if (hitbox.Value.Intersects(hurtbox.Value))
                {
                    Console.WriteLine($"{attackerId} hit {hitbox.Value}");
                }
            }
        }
    }
}