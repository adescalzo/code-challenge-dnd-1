using System.IO.Abstractions;
using System.Globalization;
using System.Text;
using System.Collections.Immutable;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal sealed class SensorCsvFileOutputService(IFileSystem fileSystem, IFileOutputSupport fileOutputSupport)
    : ISensorFileOutputService
{
    private string? _filePath;
    private StreamWriter? _writer;
    private bool _isActive = true;

    public OutputResultType OutputResult => OutputResultType.Csv;

    public async Task<Result> InitializeAsync(SensorOutputRequest outputRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            _filePath = fileOutputSupport.CreateFullFilePath(outputRequest.OutputFilePath, OutputResult);
            var stream = fileSystem.File.Create(_filePath);
            _writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 65536);

            // Write CSV header
            await _writer
                .WriteLineAsync("Index;SensorId;Value;Zone;IsActive".AsMemory(), cancellationToken)
                .ConfigureAwait(false);

            _isActive = true;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _isActive = false;
            return Result.Failure(ErrorResult.Error($"Failed to initialize CSV file: {ex.Message}", ex));
        }
    }

    public async Task WriteAsync(IImmutableList<SensorReadingModel> sensorReadings, CancellationToken cancellationToken = default)
    {
        if (!_isActive)
        {
            return;
        }

        if (_writer == null)
        {
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");
        }

        foreach (var reading in sensorReadings)
        {
            await _writer
                .WriteLineAsync(
                    $"{reading.Index};{reading.Id};{reading.Value.ToString(CultureInfo.InvariantCulture)};{reading.Zone};{reading.IsActive}".AsMemory(),
                    cancellationToken
                ).ConfigureAwait(false);
        }

        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<WriteResponse>> CloseAsync()
    {
        try
        {
            if (_writer == null)
            {
                return Result.Success(new WriteResponse(_filePath!, OutputResult));
            }

            await _writer.FlushAsync().ConfigureAwait(false);
            await _writer.DisposeAsync().ConfigureAwait(false);
            _writer = null;

            _isActive = false;
            return Result.Success(new WriteResponse(_filePath!, OutputResult));
        }
        catch (Exception ex)
        {
            var deleted = fileOutputSupport.TryToDelete(_filePath ?? string.Empty)
                ? string.Empty
                : $"Please, check to delete the file manually. {_filePath}";

            _isActive = false;
            return Result.Failure<WriteResponse>(
                ErrorResult.Error($"Failed to close CSV file: {ex.Message}. {deleted}", ex)
            );
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _writer = null;
    }
}
