using System.Collections.Immutable;

namespace TechChallenge.Application.Services.SensorReader;

public record SensorDataAccumulation(
    string MaxValueSensorId,
    double GlobalAverageValue,
    IImmutableList<ZoneInformation> ZonesInformation
);

public record ZoneInformation(
    string Zone,
    double AverageMeasurement,
    int ActiveSensors
);
