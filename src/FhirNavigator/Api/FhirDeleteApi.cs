using FhirNavigator.FhirHttpClient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Api;

public class FhirDeleteApi(IFhirHttpClientFactory fhirHttpClientFactory, ILogger<FhirGetApi> logger) : FhirApiBase, IFhirDeleteApi
{
    public async System.Threading.Tasks.Task DeleteAsync<T>(string repositoryCode, string id) where T : Resource
    {
        ThrowIfRepositoryCodeEmptyString(repositoryCode);
        FhirClient fhirClient = fhirHttpClientFactory.CreateClient(repositoryCode);
        string resourceName = GetResourceTypeName<T>();
        try
        {
            await fhirClient.DeleteAsync($"{resourceName}/{id}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "{ExceptionType} when querying external FHIR endpoint for RepositoryCode: {OrderRepositoryCode}, {Query}, ErrorMessage: {ExceptionMessage}",
                exception.GetType().Name,
                repositoryCode,
                GetQueryForLogging(GetResourceTypeName<T>(), id),
                exception.Message);
            throw;
        }
    }
    
    private static string GetQueryForLogging(string resourceName,
        string resourceId)
    {
        return $"DELETE [base]/{resourceName}/{resourceId}";
    }
}