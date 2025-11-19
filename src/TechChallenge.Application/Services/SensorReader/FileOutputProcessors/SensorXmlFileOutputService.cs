using System.Xml;
using System.IO.Abstractions;
using System.Text;
using System.Collections.Immutable;
using System.Globalization;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal sealed class SensorXmlFileOutputService(IFileSystem fileSystem, IFileOutputSupport fileOutputSupport)
    : ISensorFileOutputService
{
    private string? _filePath;
    private XmlWriter? _xmlWriter;
    private bool _isActive = true;

    public OutputResultType OutputResult => OutputResultType.Xml;

    public async Task<Result> InitializeAsync(SensorOutputRequest outputRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            _filePath = fileOutputSupport.CreateFullFilePath(outputRequest.OutputFilePath, OutputResult);
            var stream = fileSystem.File.Create(_filePath);

            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "  "
            };

            _xmlWriter = XmlWriter.Create(stream, settings);

            // Write XML declaration and root element
            await _xmlWriter.WriteStartDocumentAsync().ConfigureAwait(false);
            await _xmlWriter.WriteStartElementAsync(null, "SensorReadings", null).ConfigureAwait(false);

            _isActive = true;

            return Result.Success();
        }
        catch (Exception ex)
        {
            _isActive = false;
            return Result.Failure(ErrorResult.Error($"Failed to initialize XML file: {ex.Message}", ex));
        }
    }

    // WriteResponseAsync: should receive a collection to save, and will receive a different type of data
    public async Task WriteAsync(IImmutableList<SensorReadingModel> sensorReadings, CancellationToken cancellationToken = default)
    {
        if (!_isActive)
        {
            return;
        }

        if (_xmlWriter == null)
        {
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");
        }

        foreach (var reading in sensorReadings)
        {
            await _xmlWriter.WriteStartElementAsync(null, "SensorReading", null).ConfigureAwait(false);

            await _xmlWriter.WriteElementStringAsync(null, "Index", null, reading.Index.ToString()).ConfigureAwait(false);
            await _xmlWriter.WriteElementStringAsync(null, "Id", null, reading.Id).ConfigureAwait(false);
            await _xmlWriter.WriteElementStringAsync(null, "Value", null, reading.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            await _xmlWriter.WriteElementStringAsync(null, "Zone", null, reading.Zone).ConfigureAwait(false);
            await _xmlWriter.WriteElementStringAsync(null, "IsActive", null, reading.IsActive.ToString()).ConfigureAwait(false);

            await _xmlWriter.WriteEndElementAsync().ConfigureAwait(false);
        }

        await _xmlWriter.FlushAsync().ConfigureAwait(false);
    }

    public async Task<Result<WriteResponse>> CloseAsync()
    {
        try
        {
            if (_xmlWriter == null)
            {
                return Result.Success(new WriteResponse(_filePath!, OutputResult));
            }

            await _xmlWriter.WriteEndElementAsync().ConfigureAwait(false);
            await _xmlWriter.WriteEndDocumentAsync().ConfigureAwait(false);
            await _xmlWriter.FlushAsync().ConfigureAwait(false);

            await _xmlWriter.DisposeAsync().ConfigureAwait(false);
            _xmlWriter = null;

            return Result.Success(new WriteResponse(_filePath!, OutputResult));
        }
        catch (Exception ex)
        {
            var deleted = fileOutputSupport.TryToDelete(_filePath ?? string.Empty)
                ? string.Empty
                : $"Please, check to delete the file manually. {_filePath}";

            return Result.Failure<WriteResponse>(
                ErrorResult.Error($"Failed to close XML file: {ex.Message}. {deleted}", ex)
            );
        }
    }

    public void Dispose()
    {
        _xmlWriter?.Dispose();
        _xmlWriter = null;
    }
}
