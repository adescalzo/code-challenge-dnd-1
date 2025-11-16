using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Services;

public interface ISensorReaderService
{
    Task<Result<IReadOnlyList<SensorOutputProcessorResponse>>> ProcessAsync(
        SensorProcessRequest sensorProcessRequest,
        CancellationToken cancellationToken = default
    );
}

internal sealed class SensorReaderService(
    ISensorProcessRequestValidator sensorProcessRequestValidator,
    ISensorFileProcessor sensorFileProcessor,
    ISensorOutputProcessor sensorOutputProcessor
) : ISensorReaderService
{
    public async Task<Result<IReadOnlyList<SensorOutputProcessorResponse>>> ProcessAsync(
        SensorProcessRequest sensorProcessRequest,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var resultValidator = sensorProcessRequestValidator.Validate(sensorProcessRequest);
            if (resultValidator.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(resultValidator.Error);
            }

            var responseSensors = await sensorFileProcessor
                .ProcessAsync(sensorProcessRequest.JsonFilePath, cancellationToken)
                .ConfigureAwait(false);

            if (responseSensors.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(responseSensors.Error);
            }

            if(responseSensors.Value is null)
            {
                return Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(
                    ErrorResult.Error("Invalid response from the file analysis")
                );
            }

            var responseOutput = await sensorOutputProcessor
                .ProcessAsync(
                    responseSensors.Value,
                    sensorProcessRequest.OutputRequests,
                    cancellationToken
                ).ConfigureAwait(false);

            if (responseOutput.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(responseOutput.Error);
            }

            if(responseOutput.Value is null)
            {
                throw new AppException("Invalid response from the file analysis");
            }

            return Result.Success(responseOutput.Value!);
        }
        catch (Exception e)
        {
            return Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(
                ErrorResult.Error("Unexpected error", e)
            );
        }
    }
}
