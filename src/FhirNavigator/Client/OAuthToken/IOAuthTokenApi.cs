using FhirNavigator.Infrastructure;

namespace FhirNavigator.Client.OAuthToken
{
  public interface IOAuthTokenApi
  {
    Task<Result<ApiToken>> PostAsync(string httpClientName, FhirRepositorySettings fhirRepositorySettings);
  }
}