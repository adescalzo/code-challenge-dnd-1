using System.Globalization;
using System.Text.Json;

namespace TechChallenge.Client;

internal static class SensorDataGenerator
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a sensor data file with the specified number of zones and items per zone
    /// </summary>
    public static async Task GenerateAsync(int numberOfZones, int itemsPerZone, string outputPath)
    {
        var sensorData = new List<SensorItem>();
        var index = 0;

        for (var zone = 1; zone <= numberOfZones; zone++)
        {
            var zoneName = $"Z{zone:D2}"; // Z01, Z02, Z03, etc.

            for (var item = 0; item < itemsPerZone; item++)
            {
                var sensorItem = new SensorItem
                {
                    Index = index++,
                    Id = Guid.NewGuid().ToString(),
                    IsActive = Random.NextDouble() > 0.3, // 70% chance of being active
                    Zone = zoneName,
                    Value = GenerateRandomValue().ToString("F2", CultureInfo.InvariantCulture)
                };

                sensorData.Add(sensorItem);
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(sensorData, options);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, json);

        Console.WriteLine($"Generated sensor data file: {outputPath}");
        Console.WriteLine($"Total sensors: {sensorData.Count}");
        Console.WriteLine($"Zones: {numberOfZones} ({string.Join(", ", Enumerable.Range(1, numberOfZones).Select(z => $"Z{z:D2}"))})");
        Console.WriteLine($"Items per zone: {itemsPerZone}");
        Console.WriteLine($"Active sensors: {sensorData.Count(s => s.IsActive)}");
        Console.WriteLine($"Inactive sensors: {sensorData.Count(s => !s.IsActive)}");
    }

    /// <summary>
    /// Generates a random float value between 2000.0 and 50000.0 with 2 decimal places
    /// </summary>
    private static float GenerateRandomValue()
    {
        const float min = 2000.0f;
        const float max = 50000.0f;

        var randomValue = (float)(Random.NextDouble() * (max - min) + min);

        return (float)Math.Round(randomValue, 2);
    }

    /// <summary>
    /// Creates a simple generator from command line args
    /// Usage: dotnet run generate 5 100 "./Output/generated_sensors.json"
    /// </summary>
    public static async Task<bool> TryGenerateFromArgs(string[] args)
    {
        if (args.Length == 0 || args[0] != "generate")
            return false;

        if (args.Length < 4)
        {
            Console.WriteLine("Usage: dotnet run generate <zones> <itemsPerZone> <outputFile>");
            Console.WriteLine("Example: dotnet run generate 3 50 \"./Input/generated_sensors.json\"");

            return true;
        }

        if (!int.TryParse(args[1], out var zones) || zones <= 0)
        {
            Console.WriteLine("Invalid number of zones. Must be a positive integer.");
            return true;
        }

        if (!int.TryParse(args[2], out var itemsPerZone) || itemsPerZone <= 0)
        {
            Console.WriteLine("Invalid items per zone. Must be a positive integer.");
            return true;
        }

        var outputPath = args[3];

        try
        {
            await GenerateAsync(zones, itemsPerZone, outputPath);
            Console.WriteLine("\nGeneration completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating file: {ex.Message}");
        }

        return true;
    }

    private sealed class SensorItem
    {
        public int Index { get; set; }
        public string Id { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Zone { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
