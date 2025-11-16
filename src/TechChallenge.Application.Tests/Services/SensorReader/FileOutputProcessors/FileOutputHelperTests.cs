using System.IO.Abstractions.TestingHelpers;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services.SensorReader.FileOutputProcessors;

public class FileOutputHelperTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly FileOutputSupport _helper;

    private const string FileNameDefault = "sensor_data_20231115_143045";

    public FileOutputHelperTests()
    {
        _helper = new FileOutputSupport(_fileSystem, _clock);
    }

    [Fact]
    public void GenerateFileName_CsvType_ReturnsCorrectFormat()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 11, 15, 14, 30, 45, DateTimeKind.Utc);
        _clock.Now().Returns(fixedTime);

        // Act
        var fileName = _helper.GenerateFileName(OutputResultType.Csv);

        // Assert
        fileName.Should().Be($"{FileNameDefault}.csv");
    }

    [Fact]
    public void GenerateFileName_XmlType_ReturnsCorrectFormat()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 11, 15, 14, 30, 45, DateTimeKind.Utc);
        _clock.Now().Returns(fixedTime);

        // Act
        var fileName = _helper.GenerateFileName(OutputResultType.Xml);

        // Assert
        fileName.Should().Be($"{FileNameDefault}.xml");
    }

    [Fact]
    public void CreateFullFilePath_ValidInput_CombinesPathCorrectly()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 11, 15, 14, 30, 45, DateTimeKind.Utc);
        _clock.Now().Returns(fixedTime);

        // Act
        var fullPath = _helper.CreateFullFilePath("/output/directory", OutputResultType.Csv);

        // Assert
        fullPath.Should().Be($"/output/directory/{FileNameDefault}.csv");
    }

    [Theory]
    [InlineData(OutputResultType.Csv)]
    [InlineData(OutputResultType.Xml)]
    public void GenerateFileName_DifferentCalls_GeneratesDifferentNames(OutputResultType outputType)
    {
        // Arrange
        _clock.Now().Returns(
            new DateTime(2023, 11, 15, 14, 30, 45, DateTimeKind.Utc),
            new DateTime(2023, 11, 15, 14, 30, 46, DateTimeKind.Utc)
        );

        // Act
        var fileName1 = _helper.GenerateFileName(outputType);
        var fileName2 = _helper.GenerateFileName(outputType);

        // Assert
        fileName1.Should().NotBe(fileName2);
    }

    [Fact]
    public void CreateFullFilePath_EmptyDirectory_HandlesGracefully()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 11, 15, 14, 30, 45, DateTimeKind.Utc);
        _clock.Now().Returns(fixedTime);

        // Act
        var fullPath = _helper.CreateFullFilePath("", OutputResultType.Csv);

        // Assert
        fullPath.Should().Be($"{FileNameDefault}.csv");
    }
}
