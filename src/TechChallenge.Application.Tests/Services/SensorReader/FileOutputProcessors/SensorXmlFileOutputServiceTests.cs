using System.IO.Abstractions.TestingHelpers;
using System.Collections.Immutable;
using System.Xml.Linq;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application.Tests.Services.SensorReader.FileOutputProcessors;

public class SensorXmlFileOutputServiceTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly IFileOutputSupport _fileOutputSupport = Substitute.For<IFileOutputSupport>();
    private readonly SensorXmlFileOutputService _service;

    private const string FileNameDefault = "sensor_data_20231115_143045";

    public SensorXmlFileOutputServiceTests()
    {
        _service = new SensorXmlFileOutputService(_fileSystem, _fileOutputSupport);
    }

    [Fact]
    public async Task WriteResponseAsync_ValidData_CreatesXmlFile()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001",
            25.75,
            ImmutableList.Create(
                new ZoneInformation("Zone_A", 30.0, 3),
                new ZoneInformation("Zone_B", 21.5, 2)
            )
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Xml);
        const string expectedFilePath = $"/output/{FileNameDefault}.xml";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Xml)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);
        result.Value.OutputType.Should().Be(OutputResultType.Xml);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var xmlContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        xmlContent.Should().NotBeEmpty();

        var doc = XDocument.Parse(xmlContent);
        doc.Root.Should().NotBeNull();
        doc.Root!.Name.LocalName.Should().Be("SensorInformacion");
    }

    [Fact]
    public async Task WriteResponseAsync_EmptyZoneData_HandlesGracefully()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_001",
            0.0,
            ImmutableList<ZoneInformation>.Empty
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Xml);
        const string expectedFilePath = "/output/empty_data.xml";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Xml)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var xmlContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        xmlContent.Should().NotBeEmpty();

        var doc = XDocument.Parse(xmlContent);
        doc.Root.Should().NotBeNull();
    }

    [Fact]
    public async Task WriteResponseAsync_ValidXmlStructure_ContainsAllData()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_MAX",
            50.0,
            ImmutableList.Create(
                new ZoneInformation("Zone_C", 60.0, 1),
                new ZoneInformation("Zone_A", 40.0, 2)
            )
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Xml);
        const string expectedFilePath = "/output/structured_data.xml";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Xml)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var xmlContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);
        var doc = XDocument.Parse(xmlContent);

        doc.Root.Should().NotBeNull();
        xmlContent.Should().Contain("SENSOR_MAX");
        xmlContent.Should().Contain("50");
        xmlContent.Should().Contain("Zone_A");
        xmlContent.Should().Contain("Zone_C");

        _fileOutputSupport.Received(1).CreateFullFilePath("/output", OutputResultType.Xml);
    }

    [Fact]
    public async Task WriteResponseAsync_LargeDataset_ProcessesSuccessfully()
    {
        // Arrange
        var zones = new List<ZoneInformation>();

        for (var i = 1; i <= 50; i++)
        {
            zones.Add(new ZoneInformation(
                $"Zone_{i:D3}",
                i * 10.5,
                i % 10 + 1
            ));
        }

        var sensorData = new SensorDataAccumulation(
            "SENSOR_MAX",
            500.25,
            zones.ToImmutableList()
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Xml);
        const string expectedFilePath = "/output/large_dataset.xml";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Xml)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PathOutputFile.Should().Be(expectedFilePath);

        _fileSystem.File.Exists(expectedFilePath).Should().BeTrue();
        var xmlContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);

        // Verify it's valid XML
        var doc = XDocument.Parse(xmlContent);
        doc.Root.Should().NotBeNull();

        // Should contain substantial content
        xmlContent.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task WriteResponseAsync_SpecialCharactersInData_EscapesCorrectly()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            "SENSOR_<>&\"'",
            25.0,
            ImmutableList.Create(
                new ZoneInformation("Zone_<>&\"'", 30.0, 1)
            )
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Xml);
        const string expectedFilePath = "/output/special_chars.xml";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Xml)
            .Returns(expectedFilePath);

        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var xmlContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);

        var doc = XDocument.Parse(xmlContent);
        doc.Root.Should().NotBeNull();

        xmlContent.Should().NotContain("<>&\"'");
    }

    [Fact]
    public async Task WriteResponseAsync_NullOrEmptyValues_HandlesGracefully()
    {
        // Arrange
        var sensorData = new SensorDataAccumulation(
            string.Empty,
            double.NaN,
            ImmutableList.Create(
                new ZoneInformation("", 0.0, 0)
            )
        );

        var request = new SensorOutputRequest("/output", OutputResultType.Xml);
        const string expectedFilePath = "/output/null_values.xml";

        _fileOutputSupport.CreateFullFilePath("/output", OutputResultType.Xml)
            .Returns(expectedFilePath);
        _fileSystem.AddDirectory("/output");

        // Act
        var result = await _service.WriteResponseAsync(sensorData, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var xmlContent = await _fileSystem.File.ReadAllTextAsync(expectedFilePath);

        // Verify it's valid XML
        var doc = XDocument.Parse(xmlContent);
        doc.Root.Should().NotBeNull();
    }
}
