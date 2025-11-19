using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace TechChallenge.Application.Services.SensorReader;

internal interface ISensorFileProcessor
{
    /// <summary>
    /// Processes sensor JSON file
    /// </summary>
    IAsyncEnumerable<SensorReadingModel> ProcessAsync(string filePath, CancellationToken cancellationToken = default);
}

internal class SensorFileProcessor : ISensorFileProcessor
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IFileSystem _fileSystem;

    public SensorFileProcessor(
        IFileSystem fileSystem,
        IOptions<ProcessorOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        _fileSystem = fileSystem;
        _jsonOptions = new JsonSerializerOptions
        {
            DefaultBufferSize = options.BufferSize,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public async IAsyncEnumerable<SensorReadingModel> ProcessAsync(
        string filePath,
        [EnumeratorCancellation]CancellationToken cancellationToken = default
    )
    {
        await using var fileStream = _fileSystem.File.OpenRead(filePath);
        var readings = JsonSerializer.DeserializeAsyncEnumerable<SensorReadingModel>(
            fileStream,
            _jsonOptions,
            cancellationToken
        );

        await foreach (var reading in readings.ConfigureAwait(false))
        {
            if (reading is null)
            {
                continue;
            }

            yield return reading;
        }
    }
}
