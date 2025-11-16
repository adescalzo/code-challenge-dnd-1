using System.Xml.Serialization;
using System.IO.Abstractions;
using System.Text;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

[XmlRoot("SensorInformacion")]
public class SensorDataDto
{
    public string MaxValueSensorId { get; set; } = string.Empty;
    public double GlobalAverageValue { get; set; }

    [XmlArray("Zonas")]
    [XmlArrayItem("ZonasInformacion")]
    public ZoneInfoDto[] ZonesInformation { get; set; } = [];
}

public class ZoneInfoDto
{
    public string Zone { get; set; } = string.Empty;
    public double AverageMeasurement { get; set; }
    public int ActiveSensors { get; set; }
}

internal class SensorXmlFileOutputService(IFileSystem fileSystem, IFileOutputSupport fileOutputSupport)
    : ISensorFileOutputService
{
    private static readonly XmlSerializer Serializer = new(typeof(SensorDataDto));

    public OutputResultType OutputResult => OutputResultType.Xml;

    public async Task<Result<WriteResponse>> WriteResponseAsync(
        SensorDataAccumulation sensorDataAccumulation,
        SensorOutputRequest outputRequest,
        CancellationToken cancellationToken = default)
    {
        var filePath = fileOutputSupport.CreateFullFilePath(outputRequest.OutputFilePath, OutputResult);
        try
        {
            var dto = new SensorDataDto
            {
                MaxValueSensorId = sensorDataAccumulation.MaxValueSensorId,
                GlobalAverageValue = sensorDataAccumulation.GlobalAverageValue,
                ZonesInformation = sensorDataAccumulation.ZonesInformation
                    .Select(z => new ZoneInfoDto
                    {
                        Zone = z.Zone,
                        AverageMeasurement = z.AverageMeasurement,
                        ActiveSensors = z.ActiveSensors
                    }).ToArray()
            };

            await using var fileStream = fileSystem.File.Create(filePath);
            await using var bufferedStream = new BufferedStream(fileStream, bufferSize: 65536);
            await using var streamWriter = new StreamWriter(bufferedStream, Encoding.UTF8);

            Serializer.Serialize(streamWriter, dto);

            await streamWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            await bufferedStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success(new WriteResponse(filePath, OutputResult));
        }
        catch (Exception ex)
        {
            var deleted = fileOutputSupport.TryToDelete(filePath)
                ? string.Empty
                : $"Please, check to delete the file manually. {filePath}";

            return Result.Failure<WriteResponse>(
                ErrorResult.Error($"Failed to write XML file: {ex.Message}. {deleted}", ex)
            );
        }
    }
}
