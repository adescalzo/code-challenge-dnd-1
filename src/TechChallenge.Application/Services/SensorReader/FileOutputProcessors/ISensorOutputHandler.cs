using System.Collections.Immutable;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal interface ISensorOutputHandler
{
    ISensorOutputProcessor GetOutputProcessor(IReadOnlyList<SensorOutputRequest> outputRequests);
}

internal interface ISensorOutputProcessor : IDisposable
{
    Task ProcessAsync(
        IImmutableList<SensorReadingModel> sensorReadingModels,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SensorOutputProcessorResponse>> EndAsync();
}
