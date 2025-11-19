using System.Collections.Immutable;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services.SensorReader.FileOutputProcessors;

public sealed class SensorOutputThreadHandlerTests : IDisposable
{
    private readonly ISensorFileOutputServiceFactory
        _serviceFactory = Substitute.For<ISensorFileOutputServiceFactory>();
    private readonly SensorOutputThreadHandler _threadHandler;

    public SensorOutputThreadHandlerTests()
    {
        _threadHandler = new SensorOutputThreadHandler(_serviceFactory, Substitute.For<Microsoft.Extensions.Logging.ILogger<SensorOutputThreadHandler>>());
    }

    [Fact]
    public async Task GetOutputProcessor_SingleOutputRequest_ReturnsValidProcessor()
    {
        // Arrange
        var outputRequests = new[]
        {
            new SensorOutputRequest("/output", OutputResultType.Csv)
        };

        var mockCsvService = Substitute.For<ISensorFileOutputService>();
        var expectedWriteResponse = new WriteResponse("/output/file.csv", OutputResultType.Csv);

        _serviceFactory.CreateService(OutputResultType.Csv).Returns(mockCsvService);
        mockCsvService.InitializeAsync(outputRequests[0], Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mockCsvService.CloseAsync()
            .Returns(Result.Success(expectedWriteResponse));

        // Act
        var processor = _threadHandler.GetOutputProcessor(outputRequests);

        // Simulate processing some sensor readings
        var sensorReadings = ImmutableList.Create(
            new SensorReadingModel { Id = "SENSOR_001", Value = 25.0f, Zone = "Zone_A", IsActive = true, Index = 1 }
        );

        await processor.ProcessAsync(sensorReadings);
        var result = await processor.EndAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Success.Should().BeTrue();
        result[0].OutputFilePath.Should().Be("/output/file.csv");
        result[0].OutputType.Should().Be(OutputResultType.Csv);

        processor.Dispose();
    }

    public void Dispose()
    {
        _threadHandler.Dispose();
    }
}
