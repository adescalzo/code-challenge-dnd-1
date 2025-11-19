using System.Collections.Immutable;

namespace TechChallenge.Application.Services.SensorReader;

public record SensorDataAccumulation(
    DateTime StartProcess,
    DateTime EndProcess,
    string MaxValueSensorId,
    double GlobalAverageValue,
    int TotalInputs,
    int ActiveInputs,
    IImmutableList<ZoneInformation> ZonesInformation
)
{
    public TimeSpan Duration => EndProcess.Subtract(StartProcess);
}

public record ZoneInformation(
    string Zone,
    double AverageMeasurement,
    int ActiveSensors
);
