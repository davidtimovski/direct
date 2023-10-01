using Direct.Web.Services;

namespace Direct.Web;

public class PullOperationsCleaner : IHostedService, IDisposable
{
    private readonly IPullService _pullService;
    private readonly ILogger<PullOperationsCleaner> _logger;
    private Timer? _timer = null;

    public PullOperationsCleaner(
        IPullService pullService,
        ILogger<PullOperationsCleaner> logger)
    {
        _pullService = pullService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(PullOperationsCleaner)} started.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));

        return Task.CompletedTask;
    }

    private void DoWork(object? _)
    {
        _pullService.RemoveExpiredOperations();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(PullOperationsCleaner)} is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
