using Microsoft.Extensions.DependencyInjection;

namespace TechChallenge.Application.Services.SensorReader.FileOutputProcessors;

internal interface ISensorFileOutputServiceFactory
{
    ISensorFileOutputService CreateService(OutputResultType serviceType);
}

internal  class SensorFileOutputServiceFactory(IServiceProvider serviceProvider) : ISensorFileOutputServiceFactory
{
    public ISensorFileOutputService CreateService(OutputResultType serviceType)
    {
        var service = serviceProvider.GetKeyedService<ISensorFileOutputService>(serviceType);

        return service ??
               throw new InvalidOperationException(
                   $"Service for type {serviceType} is not registered as a keyed service"
                );
    }
}
