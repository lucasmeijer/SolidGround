using System.Threading.Channels;

class BackgroundWorkService : BackgroundService
{
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<BackgroundWorkService> _logger;
    readonly int _maxParallelJobs = 4;
    readonly SemaphoreSlim _semaphore;
    readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

    public BackgroundWorkService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundWorkService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_maxParallelJobs);
        
        _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public async ValueTask QueueWorkAsync(Func<IServiceProvider, CancellationToken, Task> workItem) 
        => await _queue.Writer.WriteAsync(workItem);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a list to keep track of running tasks
        var runningTasks = new List<Task>();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Clean up completed tasks
                runningTasks.RemoveAll(t => t.IsCompleted);

                await _semaphore.WaitAsync(stoppingToken);

                try
                {
                    var workItem = await _queue.Reader.ReadAsync(stoppingToken);
                    
                    // Start a new task to process the work item
                    var task = ProcessWorkItemAsync(workItem, stoppingToken);
                    runningTasks.Add(task);
                }
                catch (OperationCanceledException)
                {
                    _semaphore.Release();
                    break;
                }
                catch (Exception ex)
                {
                    _semaphore.Release();
                    _logger.LogError(ex, "Error reading from queue");
                }
            }

            // Wait for all running tasks to complete when stopping
            await Task.WhenAll(runningTasks);
        }
        finally
        {
            _semaphore.Dispose();
        }
    }

    private async Task ProcessWorkItemAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await workItem(scope.ServiceProvider, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing background work item");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.Complete();
        await base.StopAsync(cancellationToken);
    }
}
