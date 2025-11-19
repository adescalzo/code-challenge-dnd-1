using System.Collections.Immutable;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal interface ISensorFileOutputService : IDisposable
{
    OutputResultType OutputResult { get; }

    Task<Result> InitializeAsync(SensorOutputRequest outputRequest, CancellationToken cancellationToken = default);

    Task WriteAsync(IImmutableList<SensorReadingModel> sensorReadings, CancellationToken cancellationToken = default);

    Task<Result<WriteResponse>> CloseAsync();
}
