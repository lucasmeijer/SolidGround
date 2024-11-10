using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SendRequest
{
    public required string Url { get; init; }
    public required object Payload { get; init; }
    public required HttpMethod Method { get; init; }
    public required string ApiKey { get; init; }
}

public class SolidGroundHttpClient(HttpClient httpClient)
{
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken stoppingToken)
    {
        return await httpClient.SendAsync(httpRequestMessage, stoppingToken);
    }
}

class SolidGroundBackgroundService(
    ILogger<SolidGroundBackgroundService> logger,
    SolidGroundHttpClient solidGroundHttpClient)
    : BackgroundService
{
    readonly Channel<SendRequest> _channel = Channel.CreateUnbounded<SendRequest>(new()
    { 
        SingleReader = true,
        SingleWriter = false 
    });
    
    readonly SemaphoreSlim _processingCompleteSemaphore = new(0);
    int _pendingRequests = 0;

    public async Task Enqueue(SendRequest request)
    {
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
        var httpRequestMessage = new HttpRequestMessage(request.Method, request.Url)
        {
            Content = JsonContent.Create(request.Payload)
        };
        httpRequestMessage.Headers.Add("X-Api-Key", request.ApiKey);
        using var response = await solidGroundHttpClient.SendAsync(httpRequestMessage, stoppingToken);
        
        if (!response.IsSuccessStatusCode)
            logger.LogError($"Failed sending ing payload to {request.Url}. Status: {response.StatusCode} Body: {await response.Content.ReadAsStringAsync(stoppingToken)}");
        else
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
