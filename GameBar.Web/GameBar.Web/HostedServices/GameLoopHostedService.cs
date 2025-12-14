using GameBar.Web.Services;

namespace GameBar.Web.HostedServices;

public class GameLoopHostedService : IHostedService
{
    private readonly GameSessionManager _gameSessionManager;
    private readonly ILogger<GameLoopHostedService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(25); // 40 ticks per second

    public GameLoopHostedService(GameSessionManager gameSessionManager, ILogger<GameLoopHostedService> logger)
    {
        _gameSessionManager = gameSessionManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token), cancellationToken);
        _logger.LogInformation("Game loop started");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        if (_loopTask is not null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
        }

        _logger.LogInformation("Game loop stopped");
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = new PeriodicTimer(TickInterval);
        try
        {
            while (await stopwatch.WaitForNextTickAsync(cancellationToken))
            {
                await _gameSessionManager.TickAsync(TickInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }
}

