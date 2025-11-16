namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

public record SensorOutputProcessorResponse(
    bool Success,
    OutputResultType OutputType,
    string? OutputFilePath = null,
    string? ErrorMessage = null
);
