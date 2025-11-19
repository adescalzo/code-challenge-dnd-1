using System.IO.Abstractions.TestingHelpers;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TechChallenge.Application.Services.SensorReader;

namespace TechChallenge.Application.Tests.Services.SensorReader;

public class SensorFileProcessorTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly IOptions<ProcessorOptions> _options = Options.Create(new ProcessorOptions { BufferSize = 1024 });
    private readonly SensorFileProcessor _processor;

    public SensorFileProcessorTests()
    {
        _processor = new SensorFileProcessor(_fileSystem, _options);
    }

    [Fact]
    public async Task ProcessAsync_ValidJsonFile_ReturnsValidSensorReadings()
    {
        // Arrange
        var sensorData = new[]
        {
            new { index = 1, id = "SENSOR_001", value = 25.5f, zone = "Zone_A", isActive = true },
            new { index = 2, id = "SENSOR_002", value = 30.0f, zone = "Zone_B", isActive = false }
        };

        var json = JsonSerializer.Serialize(sensorData);
        _fileSystem.AddFile("/test/sensors.json", new MockFileData(json));

        // Act
        var readings = new List<SensorReadingModel>();
        await foreach (var reading in _processor.ProcessAsync("/test/sensors.json"))
        {
            readings.Add(reading);
        }

        // Assert
        readings.Should().HaveCount(2);

        readings[0].Id.Should().Be("SENSOR_001");
        readings[0].Value.Should().Be(25.5f);
        readings[0].Zone.Should().Be("Zone_A");
        readings[0].IsActive.Should().BeTrue();
        readings[0].Index.Should().Be(1);

        readings[1].Id.Should().Be("SENSOR_002");
        readings[1].Value.Should().Be(30.0f);
        readings[1].Zone.Should().Be("Zone_B");
        readings[1].IsActive.Should().BeFalse();
        readings[1].Index.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Act & Assert
        var act = async () =>
        {
            await foreach (var _ in _processor.ProcessAsync("/nonexistent/file.json"))
            {
                // Should not reach here
            }
        };

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ProcessAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        _fileSystem.AddFile("/test/invalid.json", new MockFileData("{ invalid json }"));

        // Act & Assert
        var act = async () =>
        {
            await foreach (var _ in _processor.ProcessAsync("/test/invalid.json"))
            {
                // Should not reach here
            }
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ProcessAsync_EmptyJsonArray_ReturnsNoReadings()
    {
        // Arrange
        _fileSystem.AddFile("/test/empty.json", new MockFileData("[]"));

        // Act
        var readings = new List<SensorReadingModel>();
        await foreach (var reading in _processor.ProcessAsync("/test/empty.json"))
        {
            readings.Add(reading);
        }

        // Assert
        readings.Should().BeEmpty();
    }
}
