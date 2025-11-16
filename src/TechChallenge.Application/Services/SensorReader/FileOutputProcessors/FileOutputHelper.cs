using System.Globalization;
using System.IO.Abstractions;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal interface IFileOutputSupport
{
    /// <summary>
    /// Generates a unique filename with timestamp
    /// </summary>
    string GenerateFileName(OutputResultType outputType);

    /// <summary>
    /// Combines the output directory path with the generated filename
    /// </summary>
    string CreateFullFilePath(string outputDirectoryPath, OutputResultType outputType);

    /// <summary>
    /// Attempts to delete a file at the specified path
    /// </summary>
    bool TryToDelete(string filePath);
}

internal class FileOutputSupport(IFileSystem fileSystem, IClock clock) : IFileOutputSupport
{
    public string GenerateFileName(OutputResultType outputType)
    {
        var timestamp = clock.Now().ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var extension = GetFileExtension(outputType);

        return $"sensor_data_{timestamp}{extension}";
    }

    public string CreateFullFilePath(string outputDirectoryPath, OutputResultType outputType)
    {
        var fileName = GenerateFileName(outputType);
        return fileSystem.Path.Combine(outputDirectoryPath, fileName);
    }

    public bool TryToDelete(string filePath)
    {
        try
        {
            if (!fileSystem.File.Exists(filePath))
            {
                return true;
            }

            fileSystem.File.Delete(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetFileExtension(OutputResultType outputType)
    {
        return outputType switch
        {
            OutputResultType.Csv => ".csv",
            OutputResultType.Xml => ".xml",
            _ => throw new ArgumentException($"Unsupported output type: {outputType}", nameof(outputType))
        };
    }
}
