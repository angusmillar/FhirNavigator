using Hl7.Fhir.Model;

namespace FhirNavigator.Api;

public interface IFhirUpdateApi
{
    Task<T> UpdateAsync<T>(string repositoryCode, T resource, bool versionAware = true) where T : Resource;
}