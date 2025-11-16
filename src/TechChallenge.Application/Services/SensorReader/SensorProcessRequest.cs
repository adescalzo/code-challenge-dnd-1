namespace TechChallenge.Application.Services.SensorReader;

public record SensorProcessRequest(string JsonFilePath, IReadOnlyList<SensorOutputRequest> OutputRequests);

public record SensorOutputRequest(string OutputFilePath, OutputResultType OutputType);

