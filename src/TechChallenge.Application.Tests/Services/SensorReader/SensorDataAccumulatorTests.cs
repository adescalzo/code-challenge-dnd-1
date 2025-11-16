using System.Collections.Immutable;
using TechChallenge.Application.Services.SensorReader;

namespace TechChallenge.Application.Tests.Services.SensorReader;

public class SensorDataAccumulatorTests
{
    private readonly SensorDataAccumulator _accumulator = new();
    private readonly ISensorDataAccumulatorReader _reader;

    public SensorDataAccumulatorTests()
    {
        _reader = _accumulator.GetReader();
    }

    [Fact]
    public void AddReading_SingleReading_UpdatesStatistics()
    {
        // Arrange
        var reading = new SensorReadingModel { Id = "SENSOR_001", Value = 25.5f, Zone = "Zone_A", IsActive = true };

        // Act
        _reader.AddReading(reading);
        var result = _reader.GetResult();

        // Assert
        result.MaxValueSensorId.Should().Be("SENSOR_001");
        result.GlobalAverageValue.Should().Be(25.5);
        var zoneA = result.ZonesInformation.Should().ContainSingle(z => z.Zone == "Zone_A").Subject;
        zoneA.AverageMeasurement.Should().Be(25.5);
        zoneA.ActiveSensors.Should().Be(1);
    }

    [Fact]
    public void AddReading_MultipleReadings_CalculatesCorrectAverages()
    {
        // Arrange
        var readings = new[]
        {
            new SensorReadingModel { Id = "SENSOR_001", Value = 20.0f, Zone = "Zone_A", IsActive = true },
            new SensorReadingModel { Id = "SENSOR_002", Value = 30.0f, Zone = "Zone_A", IsActive = true },
            new SensorReadingModel { Id = "SENSOR_003", Value = 40.0f, Zone = "Zone_B", IsActive = false }
        };

        // Act
        foreach (var reading in readings)
        {
            _reader.AddReading(reading);
        }

        var result = _reader.GetResult();

        // Assert
        result.GlobalAverageValue.Should().Be(30.0);

        var zoneA = result.ZonesInformation.Should().ContainSingle(z => z.Zone == "Zone_A").Subject;
        zoneA.AverageMeasurement.Should().Be(25.0);
        zoneA.ActiveSensors.Should().Be(2);

        var zoneB = result.ZonesInformation.Should().ContainSingle(z => z.Zone == "Zone_B").Subject;
        zoneB.AverageMeasurement.Should().Be(40.0);
        zoneB.ActiveSensors.Should().Be(0);
    }

    [Fact]
    public void AddReading_FindsMaxValue()
    {
        // Arrange
        var readings = new[]
        {
            new SensorReadingModel { Id = "SENSOR_001", Value = 20.0f, Zone = "Zone_A", IsActive = true },
            new SensorReadingModel { Id = "SENSOR_002", Value = 50.0f, Zone = "Zone_B", IsActive = true },
            new SensorReadingModel { Id = "SENSOR_003", Value = 30.0f, Zone = "Zone_C", IsActive = true }
        };

        // Act
        foreach (var reading in readings)
        {
            _reader.AddReading(reading);
        }

        var result = _reader.GetResult();

        // Assert
        result.MaxValueSensorId.Should().Be("SENSOR_002");
    }

    [Fact]
    public void AddReading_InactiveSensors_NotCountedInActiveSensors()
    {
        // Arrange
        var readings = new[]
        {
            new SensorReadingModel { Id = "SENSOR_001", Value = 20.0f, Zone = "Zone_A", IsActive = true },
            new SensorReadingModel { Id = "SENSOR_002", Value = 30.0f, Zone = "Zone_A", IsActive = false },
            new SensorReadingModel { Id = "SENSOR_003", Value = 40.0f, Zone = "Zone_A", IsActive = true }
        };

        // Act
        foreach (var reading in readings)
        {
            _reader.AddReading(reading);
        }

        var result = _reader.GetResult();

        // Assert
        var zoneA = result.ZonesInformation.Should().ContainSingle(z => z.Zone == "Zone_A").Subject;
        zoneA.ActiveSensors.Should().Be(2);
        zoneA.AverageMeasurement.Should().Be(30.0);
    }

    [Fact]
    public void GetReader_ResetsAccumulator()
    {
        // Arrange
        _reader.AddReading(
            new SensorReadingModel { Id = "SENSOR_001", Value = 25.0f, Zone = "Zone_A", IsActive = true });
        var firstResult = _reader.GetResult();

        // Act
        var newReader = _accumulator.GetReader();
        var secondResult = newReader.GetResult();

        // Assert
        firstResult.ZonesInformation.Should().ContainSingle(z => z.Zone == "Zone_A");
        secondResult.ZonesInformation.Should().BeEmpty();
        secondResult.MaxValueSensorId.Should().BeEmpty();
        secondResult.GlobalAverageValue.Should().Be(0);
    }

    [Fact]
    public void AddReading_EmptyZoneName_HandlesGracefully()
    {
        // Arrange
        var reading = new SensorReadingModel { Id = "SENSOR_001", Value = 25.0f, Zone = "", IsActive = true };

        // Act
        _reader.AddReading(reading);
        var result = _reader.GetResult();

        // Assert
        var emptyZone = result.ZonesInformation.Should().ContainSingle(z => z.Zone == "").Subject;
        emptyZone.AverageMeasurement.Should().Be(25.0);
        emptyZone.ActiveSensors.Should().Be(1);
    }
}
