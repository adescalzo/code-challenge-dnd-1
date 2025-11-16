using System.IO.Abstractions.TestingHelpers;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TechChallenge.Application.Services.SensorReader;

namespace TechChallenge.Application.Tests.Services.SensorReader;

public class SensorFileProcessorTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly ISensorDataAccumulator _accumulator = Substitute.For<ISensorDataAccumulator>();
    private readonly IOptions<ProcessorOptions> _options = Options.Create(new ProcessorOptions { BufferSize = 1024 });
    private readonly SensorFileProcessor _processor;

    public SensorFileProcessorTests()
    {
        _processor = new SensorFileProcessor(_accumulator, _fileSystem, _options);
    }

    [Fact]
    public async Task ProcessAsync_ValidJsonFile_ReturnsSuccess()
    {
        // Arrange
        var sensorData = new[]
        {
            new { Id = "SENSOR_001", Value = 25.5f, Zone = "Zone_A", IsActive = true },
            new { Id = "SENSOR_002", Value = 30.0f, Zone = "Zone_B", IsActive = false }
        };

        var json = JsonSerializer.Serialize(sensorData);
        _fileSystem.AddFile("/test/sensors.json", new MockFileData(json));

        var mockReader = Substitute.For<ISensorDataAccumulatorReader>();
        var expectedResult = new SensorDataAccumulation(
            "SENSOR_001", 27.75,
            ImmutableList.Create(
                new ZoneInformation("Zone_A", 25.5, 1),
                new ZoneInformation("Zone_B", 30.0, 0)
            )
        );

        _accumulator.GetReader().Returns(mockReader);
        mockReader.GetResult().Returns(expectedResult);

        // Act
        var result = await _processor.ProcessAsync("/test/sensors.json");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResult);
        mockReader.Received(2).AddReading(Arg.Any<SensorReadingModel>());
    }

    [Fact]
    public async Task ProcessAsync_FileNotFound_ReturnsFailure()
    {
        // Act
        var result = await _processor.ProcessAsync("/nonexistent/file.json");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FileProcess");
        result.Error.Description.Should().Contain("Sensor file not found");
    }

    [Fact]
    public async Task ProcessAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange
        _fileSystem.AddFile("/test/invalid.json", new MockFileData("{ invalid json }"));

        // Act
        var result = await _processor.ProcessAsync("/test/invalid.json");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FileProcess");
        result.Error.Description.Should().Contain("Invalid JSON format");
    }

    [Fact]
    public async Task ProcessAsync_EmptyJsonArray_ReturnsSuccess()
    {
        // Arrange
        _fileSystem.AddFile("/test/empty.json", new MockFileData("[]"));

        var mockReader = Substitute.For<ISensorDataAccumulatorReader>();
        var expectedResult = new SensorDataAccumulation(
            string.Empty, 0,
            ImmutableList<ZoneInformation>.Empty
        );

        _accumulator.GetReader().Returns(mockReader);
        mockReader.GetResult().Returns(expectedResult);

        // Act
        var result = await _processor.ProcessAsync("/test/empty.json");

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockReader.DidNotReceive().AddReading(Arg.Any<SensorReadingModel>());
    }
}
