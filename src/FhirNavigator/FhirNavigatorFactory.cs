using FhirNavigator.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FhirNavigator;

public class FhirNavigatorFactory(IServiceProvider serviceProvider) : IFhirNavigatorFactory
{
    public IFhirNavigator GetFhirNavigator(string repositoryCode)
    {
        return serviceProvider.GetRequiredKeyedService<IFhirNavigator>(repositoryCode);
    }
}