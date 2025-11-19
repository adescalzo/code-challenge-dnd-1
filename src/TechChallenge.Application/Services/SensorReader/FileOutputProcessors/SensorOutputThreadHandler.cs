using System.Collections.Immutable;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal sealed class SensorOutputThreadHandler(
    ISensorFileOutputServiceFactory sensorFileOutputServiceFactory,
    ILogger<SensorOutputThreadHandler> logger
) : ISensorOutputHandler, ISensorOutputProcessor
{
    private ImmutableList<ThreadContainer>? _threadContainers;

    public ISensorOutputProcessor GetOutputProcessor(IReadOnlyList<SensorOutputRequest> outputRequests)
    {
        _threadContainers = GetFileOutputServices(outputRequests);
        return this;
    }

    public Task ProcessAsync(IImmutableList<SensorReadingModel> sensorReadingModels, CancellationToken cancellationToken = default)
    {
        if (_threadContainers == null)
        {
            throw new InvalidOperationException("GetOutputProcessor must be called before ProcessAsync");
        }

        var activeQueues = _threadContainers
            .Select(threadContainer => threadContainer.Queue)
            .Where(queue => !queue.IsAddingCompleted);

        foreach (var queue in activeQueues)
        {
            queue.Add(sensorReadingModels, cancellationToken);
        }

        return Task.CompletedTask; // No async work needed here, just queuing
    }

    public Task<IReadOnlyList<SensorOutputProcessorResponse>> EndAsync()
    {
        var responses = new List<SensorOutputProcessorResponse>();

        try
        {
            if (_threadContainers == null)
            {
                return Task.FromResult<IReadOnlyList<SensorOutputProcessorResponse>>(responses);
            }

            foreach (var container in _threadContainers)
            {
                container.Queue.CompleteAdding();
            }

            foreach (var container in _threadContainers)
            {
                container.Thread.Join(TimeSpan.FromSeconds(10));
            }

            responses.AddRange(_threadContainers.Select(container => container.Response));

            return Task.FromResult<IReadOnlyList<SensorOutputProcessorResponse>>(responses);
        }
        catch (Exception ex)
        {
            responses.Add(new SensorOutputProcessorResponse(
                false,
                OutputResultType.Csv,  // Default fallback
                null,
                $"EndAsync failed: {ex.Message}"
            ));
            return Task.FromResult<IReadOnlyList<SensorOutputProcessorResponse>>(responses);
        }
    }

    private ImmutableList<ThreadContainer> GetFileOutputServices(IReadOnlyList<SensorOutputRequest> outputRequests)
    {
        var threadContainers = new List<ThreadContainer>();
        var processIndex = 0;

        foreach (var outputRequest in outputRequests)
        {
            var queue = new BlockingCollection<IImmutableList<SensorReadingModel>>();
            var responseContainer = new ResponseContainer();

            var thread = new Thread(() =>
            {
                var sensorFileOutputService = sensorFileOutputServiceFactory.CreateService(outputRequest.OutputType);
                try
                {
                    var initResult = sensorFileOutputService.InitializeAsync(outputRequest).GetAwaiter().GetResult();
                    if (initResult.IsFailure)
                    {
                        responseContainer.Response = new SensorOutputProcessorResponse(
                            false,
                            outputRequest.OutputType,
                            null,
                            initResult.Error?.Description
                        );
                        return;
                    }

                    foreach (var sensorReadingCollection in queue.GetConsumingEnumerable())
                    {
                        sensorFileOutputService.WriteAsync(sensorReadingCollection).GetAwaiter().GetResult();
                    }

                    var closeResult = sensorFileOutputService.CloseAsync().GetAwaiter().GetResult();
                    responseContainer.Response = new SensorOutputProcessorResponse(
                        closeResult.IsSuccess,
                        outputRequest.OutputType,
                        closeResult.Value?.PathOutputFile,
                        closeResult.Error?.Description
                    );
                }
                catch (Exception ex)
                {
                    responseContainer.Response = new SensorOutputProcessorResponse(
                        false,
                        outputRequest.OutputType,
                        null,
                        $"Processing worker error: {ex.Message}"
                    );
                }
                finally
                {
                    sensorFileOutputService?.Dispose();
                }
            })
            {
                IsBackground = true,
                Name = $"ProcessingWorker-{outputRequest.OutputType}-{processIndex++}"
            };

            thread.Start();
            threadContainers.Add(new ThreadContainer(thread, queue, responseContainer));
        }

        return threadContainers.ToImmutableList();
    }

    public void Dispose()
    {
        if (_threadContainers == null)
        {
            return;
        }

        foreach (var container in _threadContainers)
        {
            try
            {
                if (!container.Queue.IsAddingCompleted)
                {
                    container.Queue.CompleteAdding();
                }

                if (container.Thread.IsAlive)
                {
                    container.Thread.Join(TimeSpan.FromSeconds(5));
                }

                container.Queue.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during disposal");
            }
        }
    }

    private sealed class ResponseContainer
    {
        public SensorOutputProcessorResponse? Response { get; set; }
    }

    private readonly record struct ThreadContainer(Thread Thread, BlockingCollection<IImmutableList<SensorReadingModel>> Queue, ResponseContainer ResponseContainer)
    {
        public SensorOutputProcessorResponse Response => ResponseContainer.Response ?? new SensorOutputProcessorResponse(
            false,
            OutputResultType.Csv,
            null,
            "Thread completed without setting response"
        );
    }
}
