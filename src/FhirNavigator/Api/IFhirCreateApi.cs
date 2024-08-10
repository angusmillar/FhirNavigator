using Hl7.Fhir.Model;

namespace FhirNavigator.Api;

public interface IFhirCreateApi
{
    Task<T> CreateAsync<T>(string repositoryCode, T resource) where T : Resource;
}