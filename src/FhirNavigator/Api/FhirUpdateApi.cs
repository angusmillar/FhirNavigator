using FhirNavigator.FhirHttpClient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Api;

public class FhirUpdateApi(IFhirHttpClientFactory fhirHttpClientFactory, ILogger<FhirUpdateApi> logger) : FhirApiBase, IFhirUpdateApi
{
    public  async Task<T> UpdateAsync<T>(string repositoryCode, T resource) where T : Resource
    {
        ThrowIfRepositoryCodeEmptyString(repositoryCode);
        FhirClient fhirClient = fhirHttpClientFactory.CreateClient(repositoryCode);
        try
        {
            Resource? updatedResource = await fhirClient.UpdateAsync(resource, versionAware: true);
            if (updatedResource is not T updatedTypedResource)
            {
                throw new InvalidCastException(nameof(updatedResource));
            }

            return updatedTypedResource;
        }
        catch (Exception exception)
        {
            string resourceName = GetResourceTypeName<T>();
            logger.LogError(exception,
                "{ExceptionType} when querying external FHIR endpoint for RepositoryCode: {OrderRepositoryCode}, {Query}, ErrorMessage: {ExceptionMessage}",
                exception.GetType().Name,
                repositoryCode,
                GetQueryForLogging(resourceName, resource.Id),
                exception.Message);
            throw;
        }
    }
    
    private static string GetQueryForLogging(string resourceName,
        string resourceId)
    {
        return $"PUT [base]/{resourceName}/{resourceId}";
    }
}