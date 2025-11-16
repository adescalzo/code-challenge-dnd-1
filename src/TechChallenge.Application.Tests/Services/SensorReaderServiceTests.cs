using System.Collections.Immutable;
using TechChallenge.Application.Services;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services;

public class SensorReaderServiceTests
{
    private readonly ISensorProcessRequestValidator _validator = Substitute.For<ISensorProcessRequestValidator>();
    private readonly ISensorFileProcessor _fileProcessor = Substitute.For<ISensorFileProcessor>();
    private readonly ISensorOutputProcessor _outputProcessor = Substitute.For<ISensorOutputProcessor>();
    private readonly SensorReaderService _sensorReaderService;

    public SensorReaderServiceTests()
    {
        _sensorReaderService = new SensorReaderService(_validator, _fileProcessor, _outputProcessor);
    }

    [Fact]
    public async Task ProcessAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new SensorProcessRequest(
            "test.json",
            [new SensorOutputRequest("output", OutputResultType.Csv)]
        );

        var sensorData = new SensorDataAccumulation(
            "SENSOR_001",
            25.5,
            ImmutableList.Create(
                new ZoneInformation("Zone_A", 30.0, 5)
            )
        );

        var expectedResponse = new[]
        {
            new SensorOutputProcessorResponse(true, OutputResultType.Csv, "output/file.csv", string.Empty)
        };

        _validator.Validate(request).Returns(Result.Success());
        _fileProcessor.ProcessAsync(request.JsonFilePath, Arg.Any<CancellationToken>())
            .Returns(Result.Success(sensorData));
        _outputProcessor.ProcessAsync(sensorData, request.OutputRequests, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<SensorOutputProcessorResponse>>(expectedResponse));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_ValidationFails_ReturnsFailure()
    {
        // Arrange
        var request = new SensorProcessRequest("test.json", []);
        var validationError = ErrorResult.Error("Invalid request");

        _validator.Validate(request).Returns(Result.Failure(validationError));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(validationError);
    }

    [Fact]
    public async Task ProcessAsync_FileProcessingFails_ReturnsFailure()
    {
        // Arrange
        var request = new SensorProcessRequest("nonexistent.json",
            [new SensorOutputRequest("output", OutputResultType.Csv)]);
        var processingError = ErrorResult.Error("File not found");

        _validator.Validate(request).Returns(Result.Success());
        _fileProcessor.ProcessAsync(request.JsonFilePath, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SensorDataAccumulation>(processingError));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(processingError);
    }

    [Fact]
    public async Task ProcessAsync_OutputProcessingFails_ReturnsFailure()
    {
        // Arrange
        var request = new SensorProcessRequest("test.json",
            [new SensorOutputRequest("invalid/path", OutputResultType.Csv)]);

        var sensorData = new SensorDataAccumulation(
            "SENSOR_001", 25.5,
            ImmutableList<ZoneInformation>.Empty
        );

        var outputError = ErrorResult.Error("Failed to create output");

        _validator.Validate(request).Returns(Result.Success());
        _fileProcessor.ProcessAsync(request.JsonFilePath, Arg.Any<CancellationToken>())
            .Returns(Result.Success(sensorData));
        _outputProcessor.ProcessAsync(sensorData, request.OutputRequests, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<SensorOutputProcessorResponse>>(outputError));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(outputError);
    }
}
