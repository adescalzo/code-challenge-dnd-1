namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal interface ISensorOutputProcessor
{
    Task<Result<IReadOnlyList<SensorOutputProcessorResponse>>> ProcessAsync(
        SensorDataAccumulation sensorDataAccumulation,
        IReadOnlyList<SensorOutputRequest> outputRequests,
        CancellationToken cancellationToken = default
    );
}

internal class SensorOutputProcessor(
    ISensorFileOutputServiceFactory sensorFileOutputServiceFactory
) : ISensorOutputProcessor
{
    public async Task<Result<IReadOnlyList<SensorOutputProcessorResponse>>> ProcessAsync(
        SensorDataAccumulation sensorDataAccumulation,
        IReadOnlyList<SensorOutputRequest> outputRequests,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var outputServices = GetFileOutputServices(outputRequests);
            var processes = outputRequests.Select(outputRequest => new
            {
                outputRequest.OutputType,
                WriteProcess = outputServices[outputRequest.OutputType]
                    .WriteResponseAsync(sensorDataAccumulation, outputRequest, cancellationToken)
            }).ToList();

            await Task.WhenAll(processes.Select(x => x.WriteProcess)).ConfigureAwait(false);

            var results = new List<SensorOutputProcessorResponse>();
            foreach (var process in processes)
            {
                var fileGenerationResult = await process.WriteProcess.ConfigureAwait(false);
                results.Add(new SensorOutputProcessorResponse(
                        fileGenerationResult.IsSuccess,
                        process.OutputType,
                        fileGenerationResult.Value?.PathOutputFile,
                        fileGenerationResult.Error.Description
                    )
                );
            }

            return Result.Success<IReadOnlyList<SensorOutputProcessorResponse>>(results);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(
                ErrorResult.Error($"Error processing output files: {ex.Message}", ex)
            );
        }
    }

    private Dictionary<OutputResultType, ISensorFileOutputService> GetFileOutputServices(
        IReadOnlyList<SensorOutputRequest> outputRequests
    )
    {
        var uniqueOutputTypes = outputRequests.Select(request => request.OutputType).Distinct().ToList();
        var outputServices = new Dictionary<OutputResultType, ISensorFileOutputService>();
        foreach (var outputType in uniqueOutputTypes)
        {
            outputServices[outputType] = sensorFileOutputServiceFactory.CreateService(outputType);
        }

        return outputServices;
    }
}
