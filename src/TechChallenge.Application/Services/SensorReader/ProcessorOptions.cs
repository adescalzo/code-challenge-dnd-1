namespace TechChallenge.Application.Services.SensorReader;

internal class ProcessorOptions
{
    public const string Key = "Processor";

    public int BufferSize { get; set; } = 32768; // Default 32768 -> 32KB buffer
}
