using Hl7.Fhir.Model;

namespace FhirNavigator.Api;

public interface IFhirGetApi
{
    Task<T> GetAsync<T>(string repositoryCode, string id) where T : Resource;
}