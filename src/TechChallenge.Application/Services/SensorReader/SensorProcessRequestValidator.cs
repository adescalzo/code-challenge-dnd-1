namespace TechChallenge.Application.Services.SensorReader;

internal interface ISensorProcessRequestValidator
{
    Result Validate(SensorProcessRequest sensorProcessRequest);
}

internal class SensorProcessRequestValidator : ISensorProcessRequestValidator
{
    public Result Validate(SensorProcessRequest sensorProcessRequest)
    {
        var jsonFileValidation = ValidateJsonFilePath(sensorProcessRequest.JsonFilePath);
        if (jsonFileValidation.IsFailure)
        {
            return jsonFileValidation;
        }

        var outputValidation = ValidateOutputRequests(sensorProcessRequest.OutputRequests);
        return outputValidation.IsSuccess
            ? Result.Success()
            : outputValidation;
    }

    private static Result ValidateJsonFilePath(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            return Result.Failure(ErrorResult.Error("JsonFilePathEmpty", "JSON file path cannot be null or empty"));
        }

        return File.Exists(jsonFilePath)
            ? Result.Success()
            : Result.Failure(ErrorResult.Error("JsonFileNotFound", $"JSON file not found at path: {jsonFilePath}"));
    }

    private static Result ValidateOutputRequests(IReadOnlyList<SensorOutputRequest> outputRequests)
    {
        if (outputRequests.Count == 0)
        {
            return Result.Failure(ErrorResult.Error("OutputRequestsEmpty", "Output requests cannot be empty"));
        }

        // Check for duplicate output requests
        var duplicateValidation = ValidateDuplicateRequests(outputRequests);
        if (duplicateValidation.IsFailure)
        {
            return duplicateValidation;
        }

        var index = 0;
        foreach (var outputRequest in outputRequests)
        {
            var validation = ValidateOutputFilePath(outputRequest.OutputFilePath, index++);
            if (validation.IsFailure)
            {
                return validation;
            }
        }

        return Result.Success();
    }

    private static Result ValidateDuplicateRequests(IReadOnlyList<SensorOutputRequest> outputRequests)
    {
        var duplicateRequests = outputRequests
            .GroupBy(request => request)
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicateRequests.Count <= 0)
        {
            return Result.Success();
        }

        var firstDuplicate = duplicateRequests[0].Key;
        return Result.Failure(ErrorResult.Error(
            $"Duplicate output request found: OutputFilePath='{firstDuplicate.OutputFilePath}', OutputType='{firstDuplicate.OutputType}'"
        ));
    }

    private static Result ValidateOutputFilePath(string outputFilePath, int index)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            return Result.Failure(ErrorResult.Error($"Output path at index {index} cannot be null or empty"));
        }

        try
        {
            if (Path.HasExtension(outputFilePath))
            {
                return Result.Failure(
                    ErrorResult.Error(
                        $"Output path at index {index} should be a directory, not a file: {outputFilePath}"
                    )
                );
            }

            if (!Directory.Exists(outputFilePath))
            {
                return Result.Failure(
                    ErrorResult.Error($"Output directory at index {index} not found: {outputFilePath}")
                );
            }
        }
        catch (Exception ex)
        {
            return Result.Failure(
                ErrorResult.Error($"Invalid output path at index {index}: {outputFilePath}", ex)
            );
        }

        return Result.Success();
    }
}
