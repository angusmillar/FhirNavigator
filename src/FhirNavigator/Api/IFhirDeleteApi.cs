using Hl7.Fhir.Model;

namespace FhirNavigator.Api;

public interface IFhirDeleteApi
{
    System.Threading.Tasks.Task DeleteAsync<T>(string repositoryCode, string id) where T : Resource;
}