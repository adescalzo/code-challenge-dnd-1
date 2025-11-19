using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal sealed class SensorOutputTaskHandler(
    ISensorFileOutputServiceFactory sensorFileOutputServiceFactory,
    ILogger<SensorOutputTaskHandler> logger
) : ISensorOutputHandler, ISensorOutputProcessor
{
    private ImmutableList<TaskContainer>? _taskContainers;

    public ISensorOutputProcessor GetOutputProcessor(IReadOnlyList<SensorOutputRequest> outputRequests)
    {
        _taskContainers = CreateTaskContainers(outputRequests);
        return this;
    }

    public Task ProcessAsync(IImmutableList<SensorReadingModel> sensorReadingModels, CancellationToken cancellationToken = default)
    {
        if (_taskContainers == null)
        {
            throw new InvalidOperationException("GetOutputProcessor must be called before ProcessAsync");
        }

        foreach (var container in _taskContainers)
        {
            if (!container.Channel.Writer.TryWrite(sensorReadingModels))
            {
                logger.LogWarning("Failed to write to channel for {OutputType}", container.OutputType);
            }
        }

        return Task.CompletedTask; // Return immediately, just like current implementation
    }

    public async Task<IReadOnlyList<SensorOutputProcessorResponse>> EndAsync()
    {
        if (_taskContainers == null)
        {
            return Array.Empty<SensorOutputProcessorResponse>();
        }

        try
        {
            foreach (var container in _taskContainers)
            {
                container.Channel.Writer.Complete();
            }

            var responses = await Task.WhenAll(_taskContainers.Select(c => c.ProcessingTask));
            return responses;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in EndAsync");

            return _taskContainers.Select(c => new SensorOutputProcessorResponse(
                false,
                c.OutputType,
                null,
                $"EndAsync failed: {ex.Message}"
            )).ToList();
        }
    }

    private ImmutableList<TaskContainer> CreateTaskContainers(IReadOnlyList<SensorOutputRequest> outputRequests)
    {
        var containers = new List<TaskContainer>();
        var processIndex = 0;

        foreach (var outputRequest in outputRequests)
        {
            var channel = Channel.CreateUnbounded<IImmutableList<SensorReadingModel>>();
            var processingTask = ProcessOutputAsync(outputRequest, channel.Reader, processIndex++);

            containers.Add(new TaskContainer(outputRequest.OutputType, channel, processingTask));
        }

        return containers.ToImmutableList();
    }

    private async Task<SensorOutputProcessorResponse> ProcessOutputAsync(
        SensorOutputRequest request,
        ChannelReader<IImmutableList<SensorReadingModel>> reader,
        int processIndex)
    {
        var service = sensorFileOutputServiceFactory.CreateService(request.OutputType);

        try
        {
            var initResult = await service.InitializeAsync(request).ConfigureAwait(false);
            if (initResult.IsFailure)
            {
                return new SensorOutputProcessorResponse(
                    false,
                    request.OutputType,
                    null,
                    initResult.Error?.Description
                );
            }

            await foreach (var sensorReadings in reader.ReadAllAsync().ConfigureAwait(false))
            {
                await service.WriteAsync(sensorReadings).ConfigureAwait(false);
            }

            var closeResult = await service.CloseAsync().ConfigureAwait(false);
            return new SensorOutputProcessorResponse(
                closeResult.IsSuccess,
                request.OutputType,
                closeResult.Value?.PathOutputFile,
                closeResult.Error?.Description
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing error for {OutputType}-{Index}", request.OutputType, processIndex);

            return new SensorOutputProcessorResponse(
                false,
                request.OutputType,
                null,
                $"Processing worker error: {ex.Message}"
            );
        }
        finally
        {
            service?.Dispose();
        }
    }

    public void Dispose()
    {
        if (_taskContainers == null)
        {
            return;
        }

        foreach (var container in _taskContainers)
        {
            try
            {
                container.Channel.Writer.TryComplete();

                if (!container.ProcessingTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    logger.LogWarning("Task for {OutputType} did not complete within timeout", container.OutputType);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during disposal for {OutputType}", container.OutputType);
            }
        }
    }

    private readonly record struct TaskContainer(
        OutputResultType OutputType,
        Channel<IImmutableList<SensorReadingModel>> Channel,
        Task<SensorOutputProcessorResponse> ProcessingTask
    );
}
