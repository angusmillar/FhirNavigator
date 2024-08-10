using Hl7.Fhir.Rest;

namespace FhirNavigator.FhirHttpClient;

public interface IFhirHttpClientFactory
{
  FhirClient CreateClient(string orderRepositoryCode);
}