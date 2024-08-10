using System.Net;
using System.Net.Http.Headers;
using FhirNavigator.Client.OAuthToken;
using Microsoft.Extensions.Logging;
using Sonic.Fhir.Tools.Placer.Application.Infrastructure;

namespace FhirNavigator.Client.Handlers;

public class AuthenticationDelegatingHandler(
    IApiTokenStore apiTokenStore, 
    IOAuthTokenApi oAuthTokenApi, 
    ILogger<AuthenticationDelegatingHandler> logger)
    : DelegatingHandler
{
    public FhirRepositorySettings? OrderRepositorySettings { set; private get; }

    //https://stackoverflow.com/questions/56204350/how-to-refresh-a-token-using-ihttpclientfactory


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (OrderRepositorySettings is null)
        {
            throw new NullReferenceException(nameof(OrderRepositorySettings));
        }

        ApiToken? token;
        if (OrderRepositorySettings.UseOAuth2)
        {
            token = apiTokenStore.GetToken(OrderRepositorySettings.Code);
            if (token is null || token.WillExpireSoon())
            {
                logger.LogInformation("Requesting new API token for {OrderRepositoryCode}: {OrderRepositoryDisplayName}", OrderRepositorySettings.Code, OrderRepositorySettings.DisplayName);
                token = await RefreshTokenAsync(OrderRepositorySettings);
                logger.LogInformation("Obtained new API token for {OrderRepositoryCode}: {OrderRepositoryDisplayName}", OrderRepositorySettings.Code, OrderRepositorySettings.DisplayName);
                apiTokenStore.AddOrReplaceToken(OrderRepositorySettings.Code, token);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.Value);
        }

        if (OrderRepositorySettings.UseBasicAuth)
        {
            if (string.IsNullOrWhiteSpace(OrderRepositorySettings.Username))
            {
                throw new ArgumentException("When UseBasicAuth is true a Username must be provided.");
            }
            if (string.IsNullOrWhiteSpace(OrderRepositorySettings.Password))
            {
                throw new ArgumentException("When UseBasicAuth is true a Password must be provided.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Basic", 
                parameter: GetBase64UsernamePassword(
                    username: OrderRepositorySettings.Username, 
                    password: OrderRepositorySettings.Password));
            
            // request.Headers.Add(
            //     "Authorization", 
            //     GetAuthorizationToken(
            //         username: OrderRepositorySettings.Username, 
            //         password: OrderRepositorySettings.Password));
        }

        if (!string.IsNullOrWhiteSpace(OrderRepositorySettings.X_API_Key))
        {
            request.Headers.Add("x-api-key", OrderRepositorySettings.X_API_Key);
        }

        //Make the API Request
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (OrderRepositorySettings.UseOAuth2)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                token = await RefreshTokenAsync(OrderRepositorySettings);
                request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.Value);

                //Try again with a Refreshed Token
                response = await base.SendAsync(request, cancellationToken);
            }
        }

        return response;
    }

    private async Task<ApiToken> RefreshTokenAsync(FhirRepositorySettings fhirRepositorySettings)
    {
        Result<ApiToken> apiTokenResult = await oAuthTokenApi.PostAsync(fhirRepositorySettings.OAuth2ClientCode, fhirRepositorySettings);
        if (apiTokenResult.Success)
        {
            return apiTokenResult.Value;
        }

        logger.LogError("Error obtaining new API token for {OrderRepositoryCode}: {OrderRepositoryDisplayName}. ErrorMessage: {ErrorMessage}", fhirRepositorySettings.Code, fhirRepositorySettings.DisplayName, apiTokenResult.ErrorMessage);
        throw new ApplicationException(apiTokenResult.ErrorMessage);
    }

    private static string GetAuthorizationToken(string username, string password)
    {
        return $"Basic {Base64Encode($"{username}:{password}")}";
    }

    private static string GetBase64UsernamePassword(string username, string password)
    {
        return Base64Encode($"{username}:{password}");
    }
    
    private static string Base64Encode(string plainText) 
    {
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(textBytes);
    }
}