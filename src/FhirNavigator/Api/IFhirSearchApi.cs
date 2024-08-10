using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirNavigator.Api;

public interface IFhirSearchApi
{
    Task<Bundle?> GetAsync<T>(string repositoryCode, SearchParams fhirQuery) where T : Resource;
    Task<Bundle?> ContinueAsync(string repositoryCode, Bundle previousBundle);
}