using System.Collections.Immutable;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services.SensorReader.FileOutputProcessors;

public class SensorOutputProcessorTests
{
    private readonly ISensorFileOutputServiceFactory
        _serviceFactory = Substitute.For<ISensorFileOutputServiceFactory>();
    private readonly SensorOutputProcessor _processor;

    public SensorOutputProcessorTests()
    {
        _processor = new SensorOutputProcessor(_serviceFactory);
    }

    [Fact]
    public async Task ProcessAsync_SingleOutputRequest_ReturnsSuccess()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001", 25.0,
            ImmutableList.Create(
                new ZoneInformation("Zone_A", 25.0, 1)
            )
        );

        var outputRequests = new[]
        {
            new SensorOutputRequest("/output", OutputResultType.Csv)
        };

        var mockCsvService = Substitute.For<ISensorFileOutputService>();
        var expectedWriteResponse = new WriteResponse("/output/file.csv", OutputResultType.Csv);

        _serviceFactory.CreateService(OutputResultType.Csv).Returns(mockCsvService);
        mockCsvService.WriteResponseAsync(sensorData, outputRequests[0])
            .Returns(Result.Success(expectedWriteResponse));

        // Act
        var result = await _processor.ProcessAsync(sensorData, outputRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Success.Should().BeTrue();
        result.Value[0].OutputFilePath.Should().Be("/output/file.csv");
        result.Value[0].OutputType.Should().Be(OutputResultType.Csv);
    }

    [Fact]
    public async Task ProcessAsync_MultipleOutputRequests_ReturnsAllResults()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001", 25.0,
            ImmutableList.Create(
                new ZoneInformation("Zone_A", 25.0, 1)
            )
        );

        var outputRequests = new[]
        {
            new SensorOutputRequest("/output", OutputResultType.Csv),
            new SensorOutputRequest("/output", OutputResultType.Xml)
        };

        var mockCsvService = Substitute.For<ISensorFileOutputService>();
        var mockXmlService = Substitute.For<ISensorFileOutputService>();

        var csvResponse = new WriteResponse("/output/file.csv", OutputResultType.Csv);
        var xmlResponse = new WriteResponse("/output/file.xml", OutputResultType.Xml);

        _serviceFactory.CreateService(OutputResultType.Csv).Returns(mockCsvService);
        _serviceFactory.CreateService(OutputResultType.Xml).Returns(mockXmlService);

        mockCsvService.WriteResponseAsync(sensorData, outputRequests[0])
            .Returns(Result.Success(csvResponse));
        mockXmlService.WriteResponseAsync(sensorData, outputRequests[1])
            .Returns(Result.Success(xmlResponse));

        // Act
        var result = await _processor.ProcessAsync(sensorData, outputRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        result.Value![0].Success.Should().BeTrue();
        result.Value[0].OutputType.Should().Be(OutputResultType.Csv);

        result.Value[1].Success.Should().BeTrue();
        result.Value[1].OutputType.Should().Be(OutputResultType.Xml);
    }

    [Fact]
    public async Task ProcessAsync_SameOutputTypeMultipleTimes_CreatesServiceOnce()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001", 25.0,
            ImmutableList<ZoneInformation>.Empty
        );

        var outputRequests = new[]
        {
            new SensorOutputRequest("/output1", OutputResultType.Csv),
            new SensorOutputRequest("/output2", OutputResultType.Csv)
        };

        var mockCsvService = Substitute.For<ISensorFileOutputService>();
        var response1 = new WriteResponse("/output1/file.csv", OutputResultType.Csv);
        var response2 = new WriteResponse("/output2/file.csv", OutputResultType.Csv);

        _serviceFactory.CreateService(OutputResultType.Csv).Returns(mockCsvService);

        mockCsvService.WriteResponseAsync(sensorData, outputRequests[0])
            .Returns(Result.Success(response1));
        mockCsvService.WriteResponseAsync(sensorData, outputRequests[1])
            .Returns(Result.Success(response2));

        // Act
        var result = await _processor.ProcessAsync(sensorData, outputRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _serviceFactory.Received(1).CreateService(OutputResultType.Csv);

        await mockCsvService.Received(1).WriteResponseAsync(sensorData, outputRequests[0]);
        await mockCsvService.Received(1).WriteResponseAsync(sensorData, outputRequests[1]);
    }

    [Fact]
    public async Task ProcessAsync_ExceptionDuringProcessing_ReturnsFailure()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001", 25.0,
            ImmutableList<ZoneInformation>.Empty
        );

        var outputRequests = new[]
        {
            new SensorOutputRequest("/output", OutputResultType.Csv)
        };

        var mockCsvService = Substitute.For<ISensorFileOutputService>();
        _serviceFactory.CreateService(OutputResultType.Csv).Returns(mockCsvService);

        mockCsvService.WriteResponseAsync(sensorData, outputRequests[0])
            .Returns(Task.FromException<Result<WriteResponse>>(new InvalidOperationException("Unexpected error")));

        // Act
        var result = await _processor.ProcessAsync(sensorData, outputRequests);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Error processing output files");
        result.Error.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task ProcessAsync_EmptyOutputRequests_ReturnsEmptyResults()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001", 25.0,
            ImmutableList<ZoneInformation>.Empty
        );

        var outputRequests = Array.Empty<SensorOutputRequest>();

        // Act
        var result = await _processor.ProcessAsync(sensorData, outputRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
