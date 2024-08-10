using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sonic.Fhir.Tools.Placer.Application.Infrastructure;
using Sonic.Orders.Common.Api.OAuthToken;

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
        HttpResponseMessage Response = httpResponseMessageResult.Value;
        if (Response.IsSuccessStatusCode)
        {
          if (Response.Content == null)
          {
            return Result<ApiToken>.Fail($"HttpClient responded with the HTTP Status code of {Response.StatusCode} yet the response's content was found to be null.");
          }
          var ResponseContent = await Response.Content.ReadAsStringAsync();
          Sonic.Orders.Common.Api.OAuthToken.OAuthToken? OAuthToken = JsonSerializer.Deserialize<Sonic.Orders.Common.Api.OAuthToken.OAuthToken>(ResponseContent);
          if (OAuthToken is not null)
          {
            ApiToken ApiToken = new ApiToken(value: OAuthToken.access_token, scheme: OAuthToken.token_type, expiresInSec: OAuthToken.expires_in, obtainedAt: DateTime.Now);
            return Result<ApiToken>.Ok(ApiToken);
          }
          else
          {
            Logger.LogError($"The response body was unable to be parsed to an {nameof(OAuthToken)} type. Response string was : {ResponseContent}");
            return Result<ApiToken>.Fail($"The response body was unable to be parsed to an {nameof(OAuthToken)} type. Response string was : {ResponseContent}");
          }                    
        }
        else
        {
          if (Response.Content != null)
          {
            var ErrorResponseContent = await Response.Content.ReadAsStringAsync();
            Logger.LogError("Response status: {StatusCode}, Content: {Content}", Response.StatusCode, ErrorResponseContent);
            return Result<ApiToken>.Fail($"Response status: {Response.StatusCode}, Content: {ErrorResponseContent}");
          }
          else
          {
            Logger.LogError("Response status: {StatusCode}, Content: [None]", Response.StatusCode);
            return Result<ApiToken>.Fail($"Response status: {Response.StatusCode}, Content: [None]");
          }
        }
      }
      else
      {
        Logger.LogError(httpResponseMessageResult.ErrorMessage);
        return Result<ApiToken>.Fail(httpResponseMessageResult.ErrorMessage);
      }
    }
  }
}
