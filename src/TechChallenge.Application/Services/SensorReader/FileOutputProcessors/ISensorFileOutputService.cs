namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal interface ISensorFileOutputService
{
    OutputResultType OutputResult { get; }

    Task<Result<WriteResponse>> WriteResponseAsync(
        SensorDataAccumulation sensorDataAccumulation,
        SensorOutputRequest outputRequest,
        CancellationToken cancellationToken = default
    );
}
