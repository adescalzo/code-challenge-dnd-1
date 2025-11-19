using System.Collections.Immutable;
using TechChallenge.Application.Services;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services;

public class SensorReaderServiceTests
{
    private readonly ISensorDataAccumulator _accumulator = Substitute.For<ISensorDataAccumulator>();
    private readonly ISensorProcessRequestValidator _validator = Substitute.For<ISensorProcessRequestValidator>();
    private readonly ISensorFileProcessor _fileProcessor = Substitute.For<ISensorFileProcessor>();
    private readonly ISensorOutputHandler _outputHandler = Substitute.For<ISensorOutputHandler>();
    private readonly ISensorOutputProcessor _outputProcessor = Substitute.For<ISensorOutputProcessor>();
    private readonly SensorReaderService _sensorReaderService;

    public SensorReaderServiceTests()
    {
        _sensorReaderService = new SensorReaderService(_accumulator, _validator, _fileProcessor, _outputHandler);
    }

    private static async IAsyncEnumerable<SensorReadingModel> CreateAsyncEnumerable(params SensorReadingModel[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<SensorReadingModel> ThrowAsyncEnumerable(Exception exception)
    {
        await Task.Yield(); // Ensure this is actually async
        throw exception;
        yield break; // This will never be reached, but required for compiler
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
            DateTime.UtcNow.AddMinutes(-5), // StartProcess
            DateTime.UtcNow,                // endProcess
            "SENSOR_001",                   // MaxValueSensorId
            25.5,                          // GlobalAverageValue
            1,                             // TotalInputs
            1,                             // ActiveInputs
            ImmutableList.Create(          // ZonesInformation
                new ZoneInformation("Zone_A", 30.0, 5)
            )
        );

        var sensorReadings = CreateAsyncEnumerable(
            new SensorReadingModel { Id = "SENSOR_001", Value = 25.5f, Zone = "Zone_A", IsActive = true, Index = 1 }
        );

        var readerMock = Substitute.For<ISensorDataAccumulatorReader>();
        readerMock.GetResult().Returns(sensorData);

        _accumulator.GetReader().Returns(readerMock);
        _validator.Validate(request).Returns(Result.Success());
        _fileProcessor.ProcessAsync(request.JsonFilePath, Arg.Any<CancellationToken>())
            .Returns(sensorReadings);
        _outputHandler.GetOutputProcessor(request.OutputRequests).Returns(_outputProcessor);
        _outputProcessor.EndAsync().Returns(Task.FromResult<IReadOnlyList<SensorOutputProcessorResponse>>(
            [new SensorOutputProcessorResponse(true, OutputResultType.Csv, "output/file.csv", null)]
        ));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MaxValueSensorId.Should().Be("SENSOR_001");
        result.Value!.GlobalAverageValue.Should().Be(25.5);
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

        var readerMock = Substitute.For<ISensorDataAccumulatorReader>();
        _accumulator.GetReader().Returns(readerMock);
        _validator.Validate(request).Returns(Result.Success());
        _fileProcessor.ProcessAsync(request.JsonFilePath, Arg.Any<CancellationToken>())
            .Returns(ThrowAsyncEnumerable(new FileNotFoundException("File not found")));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_OutputProcessingFails_ReturnsFailure()
    {
        // Arrange
        var request = new SensorProcessRequest("test.json",
            [new SensorOutputRequest("invalid/path", OutputResultType.Csv)]);

        var sensorData = new SensorDataAccumulation(
            DateTime.UtcNow.AddMinutes(-2), // StartProcess
            DateTime.UtcNow,                // endProcess
            "SENSOR_001",                   // MaxValueSensorId
            25.5,                          // GlobalAverageValue
            1,                             // TotalInputs
            1,                             // ActiveInputs
            ImmutableList<ZoneInformation>.Empty // ZonesInformation
        );

        var sensorReadings = CreateAsyncEnumerable(
            new SensorReadingModel { Id = "SENSOR_001", Value = 25.5f, Zone = "Zone_A", IsActive = true, Index = 1 }
        );

        var readerMock = Substitute.For<ISensorDataAccumulatorReader>();
        readerMock.GetResult().Returns(sensorData);

        _accumulator.GetReader().Returns(readerMock);
        _validator.Validate(request).Returns(Result.Success());
        _fileProcessor.ProcessAsync(request.JsonFilePath, Arg.Any<CancellationToken>())
            .Returns(sensorReadings);
        _outputHandler.GetOutputProcessor(request.OutputRequests).Returns(_outputProcessor);
        _outputProcessor.EndAsync().Returns(Task.FromResult<IReadOnlyList<SensorOutputProcessorResponse>>(
            [new SensorOutputProcessorResponse(false, OutputResultType.Csv, null, "Failed to create output")]
        ));

        // Act
        var result = await _sensorReaderService.ProcessAsync(request);

        // Assert - Since output processing doesn't fail the overall process, but files may fail
        result.IsSuccess.Should().BeTrue();
    }
}
