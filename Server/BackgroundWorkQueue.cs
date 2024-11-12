using System.Threading.Channels;

class BackgroundWorkService(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundWorkService> logger)
    : BackgroundService
{
    readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    public async ValueTask QueueWorkAsync(Func<IServiceProvider, CancellationToken, Task> workItem) => await _queue.Writer.WriteAsync(workItem);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _queue.Reader.ReadAsync(stoppingToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing background work item");
            }
        }
    }
}