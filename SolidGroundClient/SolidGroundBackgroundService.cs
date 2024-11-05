using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class SendRequest
{
    public required string Url { get; init; }
    public required object Payload { get; init; }
}

public class SolidGroundBackgroundService(
    ILogger<SolidGroundBackgroundService> logger,
    IHttpClientFactory httpClientFactory)
    : BackgroundService
{
    readonly Channel<SendRequest> _channel = Channel.CreateUnbounded<SendRequest>(new()
    { 
        SingleReader = false,
        SingleWriter = false 
    });

    readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(SolidGroundBackgroundService));
    readonly SemaphoreSlim _processingCompleteSemaphore = new(0);
    int _pendingRequests = 0;

    public async Task EnqueueHttpPost(string url, object payload)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentNullException.ThrowIfNull(payload);

        var request = new SendRequest
        {
            Url = url,
            Payload = payload
        };

        Interlocked.Increment(ref _pendingRequests);
        await _channel.Writer.WriteAsync(request);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // If there are no pending requests, return immediately
        if (_pendingRequests == 0) return;

        // Wait for processing to complete
        await _processingCompleteSemaphore.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessRequestAsync(request, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing request to URL {Url}", request.Url);
                }
                finally
                {
                    var pendingCount = Interlocked.Decrement(ref _pendingRequests);
                    if (pendingCount == 0)
                    {
                        _processingCompleteSemaphore.Release();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Background service is stopping");
        }
    }

    async Task ProcessRequestAsync(SendRequest request, CancellationToken stoppingToken)
    {
        using var jsonContent = JsonContent.Create(request.Payload);
        using var response = await _httpClient.PostAsync(request.Url, jsonContent, stoppingToken);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Successfully sent payload to {Url}", request.Url);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _processingCompleteSemaphore.Dispose();
        base.Dispose();
    }
}
