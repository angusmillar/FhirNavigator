using FhirNavigator.FhirHttpClient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Api;

public class FhirGetApi(IFhirHttpClientFactory fhirHttpClientFactory, ILogger<FhirGetApi> logger) : FhirApiBase, IFhirGetApi
{
    public async Task<T> GetAsync<T>(string repositoryCode,
        string id) where T : Resource
    {
        ThrowIfRepositoryCodeEmptyString(repositoryCode);
        FhirClient fhirClient = fhirHttpClientFactory.CreateClient(repositoryCode);
        string resourceName = GetResourceTypeName<T>();
        try
        {
            Resource? resource = await fhirClient.GetAsync($"{resourceName}/{id}");
            if (resource is not T typedResource)
            {
                throw new InvalidCastException(nameof(resource));
            }

            return typedResource;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "{ExceptionType} when querying external FHIR endpoint for RepositoryCode: {OrderRepositoryCode}, {Query}, ErrorMessage: {ExceptionMessage}",
                exception.GetType().Name,
                repositoryCode,
                GetQueryForLogging(resourceName, id),
                exception.Message);
            throw;
        }
    }
    
    private static string GetQueryForLogging(string resourceName,
        string resourceId)
    {
        return $"GET [base]/{resourceName}/{resourceId}";
    }
}