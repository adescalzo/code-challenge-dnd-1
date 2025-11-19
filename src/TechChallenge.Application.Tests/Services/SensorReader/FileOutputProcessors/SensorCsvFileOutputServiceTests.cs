using System.IO.Abstractions.TestingHelpers;
using System.Collections.Immutable;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services.SensorReader.FileOutputProcessors;

public sealed class SensorCsvFileOutputServiceTests : IDisposable
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly IFileOutputSupport _fileOutputSupport = Substitute.For<IFileOutputSupport>();
    private readonly SensorCsvFileOutputService _service;

    private const string FileNameDefault = "sensor_data_20231115_143045";

    public SensorCsvFileOutputServiceTests()
    {
        _service = new SensorCsvFileOutputService(_fileSystem, _fileOutputSupport);
    }

    [Fact]
    public async Task WriteResponseAsync_ValidData_CreatesCSVFile()
    {
        // Arrange
        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = $"/output/{FileNameDefault}.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");
        _fileSystem.AddDirectory("/output");

        // Act
        var initResult = await _service.InitializeAsync(request);

        // Convert to sensor readings for the new interface
        var sensorReadings = new List<SensorReadingModel>
        {
            new() { Id = "SENSOR_001", Value = 25.5f, Zone = "Zone_A", IsActive = true, Index = 1 },
            new() { Id = "SENSOR_002", Value = 30.0f, Zone = "Zone_A", IsActive = true, Index = 2 }
        }.ToImmutableList();

        await _service.WriteAsync(sensorReadings);
        var result = await _service.CloseAsync();

        // Assert
        initResult.IsSuccess.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);
        result.Value.OutputType.Should().Be(OutputResultType.Csv);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var fileContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        fileContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WriteResponseAsync_EmptyZoneData_HandlesGracefully()
    {
        // Arrange
        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = "/output/empty_data.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var initResult = await _service.InitializeAsync(request);

        // Write empty data
        var sensorReadings = ImmutableList<SensorReadingModel>.Empty;
        await _service.WriteAsync(sensorReadings);
        var result = await _service.CloseAsync();

        // Assert
        initResult.IsSuccess.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var fileContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        fileContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WriteResponseAsync_MultipleZones_OrdersAlphabetically()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            DateTime.UtcNow.AddMinutes(-10), // StartProcess
            DateTime.UtcNow,                 // endProcess
            "SENSOR_MAX",                    // MaxValueSensorId
            50.0,                           // GlobalAverageValue
            6,                              // TotalInputs
            6,                              // ActiveInputs
            ImmutableList.Create(           // ZonesInformation
                new ZoneInformation("Zone_C", 60.0, 1),
                new ZoneInformation("Zone_A", 40.0, 2),
                new ZoneInformation("Zone_B", 50.0, 3)
            )
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = "/output/ordered_data.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var initResult = await _service.InitializeAsync(request);

        // Convert zones to individual sensor readings
        var sensorReadings = sensorData.ZonesInformation.Select((zone, index) =>
            new SensorReadingModel
            {
                Id = $"SENSOR_{index:D3}",
                Value = (float)zone.AverageMeasurement,
                Zone = zone.Zone,
                IsActive = zone.ActiveSensors > 0,
                Index = index + 1
            }).ToImmutableList();

        await _service.WriteAsync(sensorReadings);
        var result = await _service.CloseAsync();

        // Assert
        initResult.IsSuccess.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);

        var fileContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        fileContent.Should().NotBeEmpty();

        _fileOutputSupport.Received(1).CreateFullFilePath("/output", OutputResultType.Csv);
    }

    [Fact]
    public async Task WriteResponseAsync_LargeDataset_ProcessesSuccessfully()
    {
        // Arrange
        var zones = new List<ZoneInformation>();

        for (var i = 1; i <= 100; i++)
        {
            zones.Add(new ZoneInformation(
                $"Zone_{i:D3}",
                i * 10.5,
                i % 10 + 1
            ));
        }

        var sensorData = new SensorDataAccumulation(
            DateTime.UtcNow.AddMinutes(-30), // StartProcess
            DateTime.UtcNow,                 // endProcess
            "SENSOR_MAX",                    // MaxValueSensorId
            500.25,                         // GlobalAverageValue
            100,                            // TotalInputs
            100,                            // ActiveInputs
            zones.ToImmutableList()         // ZonesInformation
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = "/output/large_dataset.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var initResult = await _service.InitializeAsync(request);

        // Convert zones to individual sensor readings
        var sensorReadings = sensorData.ZonesInformation.Select((zone, index) =>
            new SensorReadingModel
            {
                Id = $"SENSOR_{index:D3}",
                Value = (float)zone.AverageMeasurement,
                Zone = zone.Zone,
                IsActive = zone.ActiveSensors > 0,
                Index = index + 1
            }).ToImmutableList();

        await _service.WriteAsync(sensorReadings);
        var result = await _service.CloseAsync();

        // Assert
        initResult.IsSuccess.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var fileContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        fileContent.Length.Should().BeGreaterThan(1000); // Should be substantial content
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}
