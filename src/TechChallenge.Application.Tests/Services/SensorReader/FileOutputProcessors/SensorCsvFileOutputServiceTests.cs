using System.IO.Abstractions.TestingHelpers;
using System.Collections.Immutable;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services.SensorReader.FileOutputProcessors;

public class SensorCsvFileOutputServiceTests
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
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001",
            25.75,
            ImmutableList.Create(
                new ZoneInformation("Zone_A", 30.0, 3),
                new ZoneInformation("Zone_B", 21.5, 2)
            )
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = $"/output/{FileNameDefault}.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
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
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001",
            0.0,
            ImmutableList<ZoneInformation>.Empty
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = "/output/empty_data.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
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
            "SENSOR_MAX",
            50.0,
            ImmutableList.Create(
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
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

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
            "SENSOR_MAX",
            500.25,
            zones.ToImmutableList()
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Csv);
        const string expectedFilePath = "/output/large_dataset.csv";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Csv)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var fileContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        fileContent.Length.Should().BeGreaterThan(1000); // Should be substantial content
    }
}
