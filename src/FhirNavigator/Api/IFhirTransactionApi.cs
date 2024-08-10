using Hl7.Fhir.Model;

namespace FhirNavigator.Api;

public interface IFhirTransactionApi
{
    Task<Bundle> CommitAsync(string repositoryCode, Bundle transactionBundle);
}