using FhirNavigator.FhirHttpClient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Api;

public class FhirTransactionApi(
    IFhirHttpClientFactory fhirHttpClientFactory, 
    ILogger<FhirUpdateApi> logger) 
    : FhirApiBase, IFhirTransactionApi
{
    public  async Task<Bundle> CommitAsync(string repositoryCode, Bundle transactionBundle)
    {
        ThrowIfRepositoryCodeEmptyString(repositoryCode);
        FhirClient fhirClient = fhirHttpClientFactory.CreateClient(repositoryCode);
        try
        {
            Bundle? transactionResponseBundle = await fhirClient.TransactionAsync(transactionBundle);
            if (transactionResponseBundle is null)
            {
                throw new NullReferenceException(nameof(transactionResponseBundle));
            }

            return transactionResponseBundle;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "{ExceptionType} when querying external FHIR endpoint for RepositoryCode: {OrderRepositoryCode}, {Query}, ErrorMessage: {ExceptionMessage}",
                exception.GetType().Name,
                repositoryCode,
                GetQueryForLogging(),
                exception.Message);
            throw;
        }
    }
    
    private static string GetQueryForLogging()
    {
        return $"POST [base]";
    }
}