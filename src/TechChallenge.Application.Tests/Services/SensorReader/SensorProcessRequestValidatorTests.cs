using System.IO.Abstractions.TestingHelpers;
using TechChallenge.Application.Services.SensorReader;

namespace TechChallenge.Application.Tests.Services.SensorReader;

public class SensorProcessRequestValidatorTests
{
    private readonly SensorProcessRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var tempJsonFile = Path.GetTempFileName();
        var tempOutputDir = Path.GetTempPath();
        File.WriteAllText(tempJsonFile, "[]");
        var validRequest = new SensorProcessRequest(
            JsonFilePath: tempJsonFile,
            OutputRequests: [new SensorOutputRequest(tempOutputDir, OutputResultType.Csv)]
        );

        try
        {
            // Act
            var result = _validator.Validate(validRequest);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            File.Delete(tempJsonFile);
        }
    }

    [Fact]
    public void Validate_EmptyJsonFilePath_ReturnsFailure()
    {
        // Arrange
        var request = new SensorProcessRequest(
            string.Empty,
            [new SensorOutputRequest("/output", OutputResultType.Csv)]
        );

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JsonFilePathEmpty");
    }

    [Fact]
    public void Validate_NonExistentJsonFile_ReturnsFailure()
    {
        // Arrange
        var request = new SensorProcessRequest(
            "/non/existent/file.json",
            [new SensorOutputRequest("/output", OutputResultType.Csv)]
        );

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JsonFileNotFound");
    }

    [Fact]
    public void Validate_EmptyOutputRequests_ReturnsFailure()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "[]");

        var request = new SensorProcessRequest(tempFile, []);

        try
        {
            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("OutputRequestsEmpty");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_DuplicateOutputRequests_ReturnsFailure()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var tempDir = Path.GetTempPath();
        File.WriteAllText(tempFile, "[]");

        var request = new SensorProcessRequest(
            tempFile,
            [
                new SensorOutputRequest(tempDir, OutputResultType.Csv),
                new SensorOutputRequest(tempDir, OutputResultType.Csv) // Duplicate
            ]
        );

        try
        {
            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Description.Should().Contain("Duplicate output request found");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_OutputPathWithFileExtension_ReturnsFailure()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "[]");

        var request = new SensorProcessRequest(
            tempFile,
            [new SensorOutputRequest("/path/to/file.csv", OutputResultType.Csv)]
        );

        try
        {
            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Description.Should().Contain("should be a directory, not a file");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_NonExistentOutputDirectory_ReturnsFailure()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "[]");

        var request = new SensorProcessRequest(
            tempFile,
            [new SensorOutputRequest("/non/existent/directory", OutputResultType.Csv)]
        );

        try
        {
            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Description.Should().Contain("Output directory at index 0 not found");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_EmptyOutputFilePath_ReturnsFailure()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "[]");

        var request = new SensorProcessRequest(
            tempFile,
            [new SensorOutputRequest("", OutputResultType.Csv)]
        );

        try
        {
            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Description.Should().Contain("Output path at index 0 cannot be null or empty");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_MultipleOutputTypes_SameDirectory_ReturnsSuccess()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var tempDir = Path.GetTempPath();
        File.WriteAllText(tempFile, "[]");

        var request = new SensorProcessRequest(
            tempFile,
            [
                new SensorOutputRequest(tempDir, OutputResultType.Csv),
                new SensorOutputRequest(tempDir, OutputResultType.Xml)
            ]
        );

        try
        {
            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
