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

    public static async Task ProcessAsync(ISensorReaderService sensorReaderService, SensorProcessRequest processRequest)
    {
        try
        {
            Console.WriteLine("Starting sensor data processing...");
            var result = await sensorReaderService.ProcessAsync(processRequest).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                Console.WriteLine("Processing completed successfully!");
                Console.WriteLine($"Generated {result.Value!.Count} output files:");

                foreach (var response in result.Value!)
                {
                    Console.WriteLine(response.Success
                        ? $"Successful: {response.OutputType}: {response.OutputFilePath}"
                        : $"Failed:     {response.OutputType}: Failed to generate output. {response.ErrorMessage}");
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
