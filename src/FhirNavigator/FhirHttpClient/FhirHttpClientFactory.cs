using FhirNavigator.Exceptions;
using Hl7.Fhir.Rest;

namespace FhirNavigator.FhirHttpClient;

public class FhirHttpClientFactory(IHttpClientFactory httpClientFactory) : IFhirHttpClientFactory
{
  public FhirClient CreateClient(string orderRepositoryCode)
  {
    HttpClient httpClient = GetHttpClient();

    SearchParameterHandling searchParameterHandling = SearchParameterHandling.Lenient;
  
    var fhirClientSettings = new FhirClientSettings()
    {
      PreferredFormat = ResourceFormat.Json,
      PreferredParameterHandling = searchParameterHandling
    };
    
    return new FhirClient(httpClient.BaseAddress, httpClient, fhirClientSettings);
    
    
    HttpClient GetHttpClient()
    {
      HttpClient httpClient = httpClientFactory.CreateClient(orderRepositoryCode);
      if (httpClient is null)
      {
        throw new FhirNavigatorException($"Unable to locate FHIR Navigator HttpClient for the repository configuration code: {orderRepositoryCode}");
      }
      if (httpClient.BaseAddress is null)
      {
        throw new FhirNavigatorException($"Not Service Base address could be found for {nameof(orderRepositoryCode)}: {orderRepositoryCode}.");
      }
      return httpClient;
    }
  }
}