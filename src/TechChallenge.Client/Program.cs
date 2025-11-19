using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Abstractions;
using TechChallenge.Client;
using TechChallenge.Application;
using TechChallenge.Application.Services;

// Check if this is a generator command
if (await SensorDataGenerator.TryGenerateFromArgs(args).ConfigureAwait(false))
{
    return;
}

// Ask user to choose between Task and Thread implementations
Console.WriteLine("=== Sensor Output Handler Selection ===");
Console.WriteLine("Choose the output processing implementation:");
Console.WriteLine("1. Task-based handler (Modern, async/await, better performance)");
Console.WriteLine("2. Thread-based handler (Traditional, blocking operations)");
Console.Write("Enter your choice (1 or 2) [Default: 1]: ");

var input = Console.ReadLine();
var useTask = input switch
{
    "2" => false,
    _ => true // Default to Task-based (both empty string and "1")
};

var handlerType = useTask ? "Task-based" : "Thread-based";
Console.WriteLine($"Selected: {handlerType} handler");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSensorDependencyInjection(builder.Configuration, useTask);

var host = builder.Build();

// Read configuration from an input file
const string configPath = "./Input/process-config.json";
var loadRequest = await ProcessSupport.LoadRequest(configPath).ConfigureAwait(false);

if (!loadRequest.IsSuccessful || loadRequest.ProcessRequest is null)
{
    var ok = loadRequest.IsSuccessful ? "Yes" : "No";
    var loadData = loadRequest.ProcessRequest is null ? "No" : "Yes";
    Console.WriteLine($"Failed to load configuration. Exiting. OK: {ok}. Loaded Data: {loadData}");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();

    return;
}

// Get the service and process
var sensorReaderService = host.Services.GetRequiredService<ISensorReaderService>();
await ProcessSupport.ProcessAsync(sensorReaderService, loadRequest.ProcessRequest, useTask).ConfigureAwait(false);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
