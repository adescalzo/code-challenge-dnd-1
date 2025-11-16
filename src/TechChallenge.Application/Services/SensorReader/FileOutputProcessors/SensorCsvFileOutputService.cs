using System.IO.Abstractions;
using System.Globalization;
using System.Text;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal class SensorCsvFileOutputService(IFileSystem fileSystem, IFileOutputSupport fileOutputSupport)
    : ISensorFileOutputService
{
    public OutputResultType OutputResult => OutputResultType.Csv;

    public async Task<Result<WriteResponse>> WriteResponseAsync(
        SensorDataAccumulation sensorDataAccumulation,
        SensorOutputRequest outputRequest,
        CancellationToken cancellationToken = default)
    {
        var filePath = fileOutputSupport.CreateFullFilePath(outputRequest.OutputFilePath, OutputResult);
        try
        {
            await using var stream = fileSystem.File.Create(filePath);
            await using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 65536);

            await writer.WriteLineAsync(
                "MaxValueSensorId;GlobalAverageValue".AsMemory(),
                cancellationToken
            ).ConfigureAwait(false);

            await writer.WriteLineAsync(
                $"{sensorDataAccumulation.MaxValueSensorId};{sensorDataAccumulation.GlobalAverageValue.ToString(CultureInfo.InvariantCulture)}".AsMemory(),
                cancellationToken
            ).ConfigureAwait(false);

            // Separator line
            await writer.WriteLineAsync("---".AsMemory(), cancellationToken).ConfigureAwait(false);

            await writer.WriteLineAsync("Zone;Average;ActiveSensors".AsMemory(), cancellationToken).ConfigureAwait(false);

            foreach (var item in sensorDataAccumulation.ZonesInformation)
            {
                await writer.WriteLineAsync(
                    $"{item.Zone};{item.AverageMeasurement};{item.ActiveSensors}".AsMemory(),
                    cancellationToken
                ).ConfigureAwait(false);
            }

            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success(new WriteResponse(filePath, OutputResult));
        }
        catch (Exception ex)
        {
            var deleted = fileOutputSupport.TryToDelete(filePath)
                ? string.Empty
                : $"Please, check to delete the file manually. {filePath}";

            return Result.Failure<WriteResponse>(
                ErrorResult.Error($"Failed to write CSV file: {ex.Message}. {deleted}", ex)
            );
        }
    }
}
