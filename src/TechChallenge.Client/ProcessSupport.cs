using System.Text.Json;
using System.Text.Json.Serialization;
using TechChallenge.Application.Services;
using TechChallenge.Application.Services.SensorReader;

namespace TechChallenge.Client;

internal static class ProcessSupport
{
    private static readonly JsonSerializerOptions JsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<(bool IsSuccessful, SensorProcessRequest? ProcessRequest)> LoadRequest(string configPath)
    {
        try
        {
            var configJson = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            var processRequest = JsonSerializer.Deserialize<SensorProcessRequest>(configJson, JsonOptions)
                                 ?? throw new InvalidOperationException("Failed to deserialize configuration");

            Console.WriteLine($"Loaded configuration from: {configPath}");
            Console.WriteLine($"Input file: {processRequest.JsonFilePath}");
            Console.WriteLine($"Output requests: {processRequest.OutputRequests.Count}");

            return (true, processRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load configuration from {configPath}: {ex.Message}");
            return (false, null);
        }

    }

    public static async Task ProcessAsync(
        ISensorReaderService sensorReaderService,
        SensorProcessRequest processRequest,
        bool useTask)
    {
        try
        {
            Console.WriteLine("Starting sensor data processing...");
            var result = await sensorReaderService.ProcessAsync(processRequest).ConfigureAwait(false);

            if (result is { IsSuccess: true, Value: not null })
            {
                Console.WriteLine("Processing completed successfully!\r\n");
                Console.WriteLine($"Processed sensor data. Using '{(useTask ? "Tasks" : "Threads")}':");
                Console.WriteLine("---------------------");
                Console.WriteLine($"Start: {result.Value.StartProcess:hh:mm:ss tt z}");
                Console.WriteLine($"End: {result.Value.EndProcess:hh:mm:ss tt z}");
                Console.WriteLine($"Duration: {result.Value.Duration}");
                Console.WriteLine($"Total inputs: {result.Value.TotalInputs}");
                Console.WriteLine($"Active inputs: {result.Value.ActiveInputs}");
                Console.WriteLine($"Max Value Sensor ID: {result.Value.MaxValueSensorId}");
                Console.WriteLine($"Max Value Sensor ID: {result.Value.MaxValueSensorId}");
                Console.WriteLine($"Global Average Value: {result.Value.GlobalAverageValue:F2}");
                Console.WriteLine($"Zone Information Count: {result.Value.ZonesInformation.Count}");

                foreach (var zone in result.Value.ZonesInformation)
                {
                    Console.WriteLine($"\tZone {zone.Zone}: Avg={zone.AverageMeasurement:F2}, Active Sensors={zone.ActiveSensors}");
                }

                return;
            }

            Console.WriteLine($"Processing failed: {result.Error.Description}");
            if (result.Error.Exception != null)
            {
                Console.WriteLine($"Exception: {result.Error.Exception.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
