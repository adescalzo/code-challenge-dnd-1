using System.Collections.Immutable;

namespace TechChallenge.Application.Services.SensorReader;

internal interface ISensorDataAccumulator
{
    /// <summary>
    /// Gets a reader interface for this accumulator
    /// </summary>
    ISensorDataAccumulatorReader GetReader();
}

internal interface ISensorDataAccumulatorReader
{
    /// <summary>
    /// Adds a single sensor reading and updates all accumulated values
    /// </summary>
    void AddReading(SensorReadingModel sensor);

    /// <summary>
    /// Get the accumulated data
    /// </summary>
    SensorDataAccumulation GetResult();
}

internal class SensorDataAccumulator : ISensorDataAccumulator, ISensorDataAccumulatorReader
{
    private readonly Dictionary<string, ZoneStatistics> _zoneStatistics = [];
    private readonly Dictionary<string, int> _activeSensorsByZone = [];

    private double _totalSum = int.MinValue;
    private int _totalCount = int.MinValue;
    private string _maxValueSensorId = string.Empty;
    private float _maxValue = float.MinValue;

    public ISensorDataAccumulatorReader GetReader()
    {
        _zoneStatistics.Clear();
        _activeSensorsByZone.Clear();
        _maxValueSensorId = string.Empty;
        _maxValue = 0f;
        _totalSum = 0;
        _totalCount = 0;

        return this;
    }

    public void AddReading(SensorReadingModel sensor)
    {
        if (sensor.Value > _maxValue)
        {
            _maxValue = sensor.Value;
            _maxValueSensorId = sensor.Id;
        }

        _totalSum += sensor.Value;
        _totalCount++;

        var zone = sensor.Zone;
        if (_zoneStatistics.TryGetValue(zone, out var zoneStats))
        {
            zoneStats.Add(sensor.Value, 1);
            _zoneStatistics[zone] = zoneStats;
        }
        else
        {
            _zoneStatistics[zone] = new ZoneStatistics(sensor.Value, 1);
        }

        _activeSensorsByZone.TryAdd(zone, 0);
        if (!sensor.IsActive)
        {
            return;
        }

        _activeSensorsByZone[zone]++;
    }

    public SensorDataAccumulation GetResult()
    {
        var globalAverage = Math.Round(_totalCount > 0 ? _totalSum / _totalCount : 0, 2);
        var zonesInformation =
            _zoneStatistics.Select(x =>
                new ZoneInformation(
                    x.Key,
                    Math.Round(x.Value.Average, 2),
                    _activeSensorsByZone.GetValueOrDefault(x.Key, 0)
                )
            );

        return new SensorDataAccumulation(
            _maxValueSensorId,
            globalAverage,
            zonesInformation.ToImmutableList()
        );
    }

    private struct ZoneStatistics(float sum, int count)
    {
        private double _sum = sum;
        private int _count = count;

        public void Add(float value, int count)
        {
            _sum += value;
            _count += count;
        }

        public readonly double Average => _count > 0 ? _sum / _count : 0;
    }
}
