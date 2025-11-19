using System.Collections.Concurrent;
using System.Collections.Immutable;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Services;

public interface ISensorReaderService
{
    Task<Result<SensorDataAccumulation>> ProcessAsync(
        SensorProcessRequest sensorProcessRequest,
        CancellationToken cancellationToken = default
    );
}

internal sealed class SensorReaderService(
    ISensorDataAccumulator accumulator,
    ISensorProcessRequestValidator sensorProcessRequestValidator,
    ISensorFileProcessor sensorFileProcessor,
    ISensorOutputHandler sensorOutputHandler
) : ISensorReaderService
{
    private const  int BatchReaders = 500;

    public async Task<Result<SensorDataAccumulation>> ProcessAsync(
        SensorProcessRequest sensorProcessRequest,
        CancellationToken cancellationToken = default
    )
    {
        var resultValidator = sensorProcessRequestValidator.Validate(sensorProcessRequest);
        if (resultValidator.IsFailure)
        {
            return Result.Failure<SensorDataAccumulation>(resultValidator.Error);
        }

        var accumulatorReader = accumulator.GetReader();
        ISensorOutputProcessor? sensorOutputProcessor = null;

        try
        {
            // Initialize output processor
            sensorOutputProcessor = sensorOutputHandler.GetOutputProcessor(sensorProcessRequest.OutputRequests);

            // Process sensor readings first
            var readings = new List<SensorReadingModel>();
            await foreach(var reading in sensorFileProcessor
                              .ProcessAsync(sensorProcessRequest.JsonFilePath, cancellationToken)
                              .ConfigureAwait(false))
            {
                readings.Add(reading);

                if (readings.Count >= BatchReaders)
                {
                    await sensorOutputProcessor
                        .ProcessAsync(readings.ToImmutableList(), cancellationToken)
                        .ConfigureAwait(false);

                    readings.Clear();
                }

                accumulatorReader.AddReading(reading);
            }

            if (readings.Count > 0)
            {
                await sensorOutputProcessor
                    .ProcessAsync(readings.ToImmutableList(), cancellationToken)
                    .ConfigureAwait(false);
            }

            await sensorOutputProcessor.EndAsync().ConfigureAwait(false);
            var sensorDataAccumulation = accumulatorReader.GetResult();

            return Result.Success(sensorDataAccumulation);
        }
        catch (Exception e)
        {
            if (sensorOutputProcessor == null)
            {
                return Result.Failure<SensorDataAccumulation>(ErrorResult.Error("Unexpected error", e));
            }

            try
            {
                await sensorOutputProcessor.EndAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupException)
            {
                // Log cleanup error but don't override original error
                Console.WriteLine($"Cleanup error during exception handling: {cleanupException.Message}");
            }

            return Result.Failure<SensorDataAccumulation>(ErrorResult.Error("Unexpected error", e));
        }
        finally
        {
            // Ensure disposal happens in all cases
            sensorOutputProcessor?.Dispose();
        }
    }
}
