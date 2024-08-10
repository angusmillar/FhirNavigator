using FhirNavigator.FhirHttpClient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Api;

public class FhirCreateApi(IFhirHttpClientFactory fhirHttpClientFactory, ILogger<FhirUpdateApi> logger) 
    : FhirApiBase, IFhirCreateApi
{
    public  async Task<T> CreateAsync<T>(string repositoryCode, T resource) where T : Resource
    {
        ThrowIfRepositoryCodeEmptyString(repositoryCode);
        FhirClient fhirClient = fhirHttpClientFactory.CreateClient(repositoryCode);
        try
        {
            Resource? createdResource = await fhirClient.CreateAsync(resource);
            if (createdResource is not T updatedTypedResource)
            {
                throw new InvalidCastException(nameof(createdResource));
            }

            return updatedTypedResource;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "{ExceptionType} when querying external FHIR endpoint for RepositoryCode: {OrderRepositoryCode}, {Query}, ErrorMessage: {ExceptionMessage}",
                exception.GetType().Name,
                repositoryCode,
                GetQueryForLogging(GetResourceTypeName<T>()),
                exception.Message);
            throw;
        }
    }
    
    private static string GetQueryForLogging(string resourceName)
    {
        return $"POST [base]/{resourceName}";
    }
}