using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Abstractions;
using Microsoft.Extensions.Options;

namespace TechChallenge.Application.Services.SensorReader;

internal interface ISensorFileProcessor
{
    /// <summary>
    /// Processes sensor JSON file
    /// </summary>
    Task<Result<SensorDataAccumulation>> ProcessAsync(string filePath, CancellationToken cancellationToken = default);
}

internal class SensorFileProcessor : ISensorFileProcessor
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISensorDataAccumulator _accumulator;
    private readonly IFileSystem _fileSystem;

    public SensorFileProcessor(ISensorDataAccumulator
        accumulator,
        IFileSystem fileSystem,
        IOptions<ProcessorOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        _accumulator = accumulator;
        _fileSystem = fileSystem;
        _jsonOptions = new JsonSerializerOptions
        {
            DefaultBufferSize = options.BufferSize,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public async Task<Result<SensorDataAccumulation>> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var reader = _accumulator.GetReader();

        try
        {
            await using var fileStream = _fileSystem.File.OpenRead(filePath);
            var readings = JsonSerializer
                .DeserializeAsyncEnumerable<SensorReadingModel>(fileStream, _jsonOptions, cancellationToken);

            await foreach (var reading in readings.ConfigureAwait(false))
            {
                if (reading != null)
                {
                    reader.AddReading(reading);
                }
            }

            return Result.Success(reader.GetResult());
        }
        catch (JsonException ex)
        {
            return Result.Failure<SensorDataAccumulation>(
                ErrorResult.Error("FileProcess", $"Invalid JSON format: {ex.Message}", ex)
            );
        }
        catch (FileNotFoundException ex)
        {
            return Result.Failure<SensorDataAccumulation>(
                ErrorResult.Error("FileProcess", $"Sensor file not found: {ex.Message}", ex)
            );
        }
        catch (Exception ex)
        {
            return Result.Failure<SensorDataAccumulation>(
                ErrorResult.Error("FileProcess", $"Error processing sensor file: {ex.Message}", ex)
            );
        }
    }
}
