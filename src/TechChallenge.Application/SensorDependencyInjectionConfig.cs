using System.ComponentModel.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechChallenge.Application.Services;
using TechChallenge.Application.Services.SensorReader;
using TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

namespace TechChallenge.Application;

public static class SensorDependencyInjectionConfig
{
    public static IServiceCollection AddSensorDependencyInjection(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useTask)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<ProcessorOptions>(configuration.GetSection(ProcessorOptions.Key));
        services.AddScoped<ISensorProcessRequestValidator, SensorProcessRequestValidator>();
        services.AddScoped<ISensorReaderService, SensorReaderService>();
        services.AddScoped<ISensorDataAccumulator, SensorDataAccumulator>();
        services.AddScoped<ISensorFileProcessor, SensorFileProcessor>();
        if (useTask)
        {
            services.AddScoped<ISensorOutputHandler, SensorOutputTaskHandler>();
        }
        else
        {
            services.AddScoped<ISensorOutputHandler, SensorOutputThreadHandler>();
        }
        services.AddScoped<IFileOutputSupport, FileOutputSupport>();
        services.AddScoped<IClock, Clock>();

        // Register file services resolve
        services.AddKeyedScoped<ISensorFileOutputService, SensorCsvFileOutputService>(OutputResultType.Csv);
        services.AddKeyedScoped<ISensorFileOutputService, SensorXmlFileOutputService>(OutputResultType.Xml);

        // Register file services factory
        services.AddScoped<ISensorFileOutputServiceFactory, SensorFileOutputServiceFactory>();

        return services;
    }
}
