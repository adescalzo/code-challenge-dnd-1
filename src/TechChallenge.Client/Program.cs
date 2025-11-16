using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Abstractions;
using TechChallenge.Client;
using TechChallenge.Application;
using TechChallenge.Application.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSensorDependencyInjection(builder.Configuration);

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
await ProcessSupport.ProcessAsync(sensorReaderService, loadRequest.ProcessRequest).ConfigureAwait(false);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
