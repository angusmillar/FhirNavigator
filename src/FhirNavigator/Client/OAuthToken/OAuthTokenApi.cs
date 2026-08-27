using System.Text.Json;
using FhirNavigator.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Client.OAuthToken
{
  public class OAuthTokenApi : HttpClientBase, IOAuthTokenApi
  {
    public OAuthTokenApi(IHttpClientFactory httpClientFactory, ILogger<OAuthTokenApi> logger)
      : base(httpClientFactory, logger) { }
    public async Task<Result<ApiToken>> PostAsync(string httpClientName, FhirRepositorySettings fhirRepositorySettings)
    {
      Result<HttpResponseMessage> httpResponseMessageResult = await base.RetryEnabledSendAsync(httpClientName, () =>
      {
        var parameterDictionary = new Dictionary<string, string>();
        parameterDictionary.Add("client_id", fhirRepositorySettings.ClientId);
        parameterDictionary.Add("client_secret", fhirRepositorySettings.ClientSecret);
        parameterDictionary.Add("grant_type", "client_credentials");
        if (!string.IsNullOrWhiteSpace(fhirRepositorySettings.Scopes))
        {
          parameterDictionary.Add("scope", fhirRepositorySettings.Scopes);
        }

        var request = new HttpRequestMessage(method: HttpMethod.Post, requestUri: (string?)null);
        request.Content = new FormUrlEncodedContent(parameterDictionary);
        return request;
      });

      if (httpResponseMessageResult.Success)
      {
        HttpResponseMessage response = httpResponseMessageResult.Value;
        if (response.IsSuccessStatusCode)
        {
          if (response.Content == null)
          {
            return Result<ApiToken>.Fail($"HttpClient responded with the HTTP Status code of {response.StatusCode} yet the response's content was found to be null.");
          }
          var responseContent = await response.Content.ReadAsStringAsync();
          Sonic.Orders.Common.Api.OAuthToken.OAuthToken? oAuthToken = JsonSerializer.Deserialize<Sonic.Orders.Common.Api.OAuthToken.OAuthToken>(responseContent);
          if (oAuthToken is not null)
          {
            ApiToken apiToken = new ApiToken(value: oAuthToken.access_token, scheme: oAuthToken.token_type, expiresInSec: oAuthToken.expires_in, obtainedAt: DateTime.Now);
            return Result<ApiToken>.Ok(apiToken);
          }
          
          Logger.LogError("The response body was unable to be parsed to an {OAuthToken} type. Response string was : {ResponseContent}",
            nameof(oAuthToken), responseContent);
          return Result<ApiToken>.Fail($"The response body was unable to be parsed to an {nameof(oAuthToken)} type. Response string was : {responseContent}");
                              
        }
       
        if (response.Content != null)
        {
          var errorResponseContent = await response.Content.ReadAsStringAsync();
          Logger.LogError("Response status: {StatusCode}, Content: {Content}", response.StatusCode, errorResponseContent);
          return Result<ApiToken>.Fail($"Response status: {response.StatusCode}, Content: {errorResponseContent}");
        }
       
        Logger.LogError("Response status: {StatusCode}, Content: [None]", response.StatusCode);
        return Result<ApiToken>.Fail($"Response status: {response.StatusCode}, Content: [None]");
      }
      
      Logger.LogError("ErrorMessage: {ErrorMessage}", httpResponseMessageResult.ErrorMessage);
      return Result<ApiToken>.Fail(httpResponseMessageResult.ErrorMessage);
      
    }
  }
}
