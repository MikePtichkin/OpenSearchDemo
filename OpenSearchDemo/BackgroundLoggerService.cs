namespace OpenSearchDemo;

public class BackgroundLoggerService : IHostedService, IDisposable
{
    private readonly ILogger<BackgroundLoggerService> _logger;
    private Timer? _timer;
    private int _count;

    public BackgroundLoggerService(ILogger<BackgroundLoggerService> logger)
    {
        _logger = logger;
        _count = 0;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting background logging service");
        _timer = new Timer(LogCurrentTime, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
        return Task.CompletedTask;
    }

    private void LogCurrentTime(object? state)
    {
        try
        {
            _logger.LogInformation("Current time: {DateTime}, Iteration: {Count}", DateTime.UtcNow.ToString("O"), ++_count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background logging");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping background logging service");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
