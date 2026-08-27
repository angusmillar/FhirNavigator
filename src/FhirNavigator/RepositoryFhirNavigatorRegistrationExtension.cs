using System.Net.Http.Headers;
using FhirNavigator.Api;
using FhirNavigator.Client;
using FhirNavigator.Client.Handlers;
using FhirNavigator.Client.OAuthToken;
using FhirNavigator.Factories;
using FhirNavigator.FhirCallService;
using FhirNavigator.FhirHttpClient;
using FhirNavigator.FhirSupport;
using FhirNavigator.FhirUri;
using FhirNavigator.Proxy;
using FhirNavigator.ResourceSearchCache;
using Microsoft.Extensions.DependencyInjection;

namespace FhirNavigator;

public static class RepositoryFhirNavigatorRegistrationExtension
{
    public static void AddFhirNavigator(this IServiceCollection services, Action<FhirNavigatorSettings> settings)
    {
        var fhirNavigatorSettings = FhirNavigatorSettings.GetDefault();
        settings(fhirNavigatorSettings);

        services.AddSingleton(fhirNavigatorSettings);
        services.AddTransient<ProxyHttpClientHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<AuthenticationDelegatingHandler>();
        services.AddTransient<IAuthenticationDelegatingHandlerFactory, AuthenticationDelegatingHandlerFactory>();
        services.AddTransient<IOAuthTokenApi, OAuthTokenApi>();
        services.AddSingleton<IApiTokenStore, ApiTokenStore>();
        services.AddSingleton<IJitter, Jitter>();
        
        foreach (FhirRepositorySettings orderRepositorySettings in fhirNavigatorSettings.FhirRepositories)
        {
            ProductInfoHeaderValue productInfoHeaderValue = new ProductInfoHeaderValue(
                fhirNavigatorSettings.UserAgentName,
                fhirNavigatorSettings.UserAgentVersion);
            
            //Set-up IHttpClientFactory to obtain HttpClients for each FHIR Order Repository
            services.AddHttpClient(orderRepositorySettings.Code,
                    client =>
                    {
                        client.BaseAddress = orderRepositorySettings.ServiceBaseUrl;
                        client.DefaultRequestHeaders.UserAgent.Add(productInfoHeaderValue);
                    })
                .ConfigurePrimaryHttpMessageHandler<ProxyHttpClientHandler>()
                .AddHttpMessageHandler<RetryDelegatingHandler>()
                .AddHttpMessageHandler(sp =>
                    sp.GetRequiredService<IAuthenticationDelegatingHandlerFactory>().Get(orderRepositorySettings));

            //Set-up IHttpClientFactory to obtain HttpClients for the OAuth token endpoints where OAuth2 is in use
            if (orderRepositorySettings.UseOAuth2)
            {
                services.AddHttpClient(orderRepositorySettings.OAuth2ClientCode,
                        client =>
                        {
                            client.BaseAddress = orderRepositorySettings.TokenEndpointUrl; 
                            client.DefaultRequestHeaders.UserAgent.Add(productInfoHeaderValue);
                        })
                    .AddHttpMessageHandler<RetryDelegatingHandler>()
                    .ConfigurePrimaryHttpMessageHandler<ProxyHttpClientHandler>();
            }

            services.AddKeyedTransient<IFhirNavigator>(orderRepositorySettings.Code, (x,
                y) => new FhirNavigator(
                fhirCallService: services.BuildServiceProvider().GetRequiredService<IFhirCallService>(),
                fhirUriFactory: services.BuildServiceProvider().GetRequiredService<IFhirUriFactory>(),
                fhirRepositorySettings: orderRepositorySettings));
        }

        //FHIR Api Client & Services
        //Scoped
        services.AddSingleton<IFhirHttpClientFactory, FhirHttpClientFactory>();
        services.AddSingleton<IFhirSearchApi, FhirSearchApi>();
        services.AddSingleton<IFhirGetApi, FhirGetApi>();
        services.AddSingleton<IFhirCreateApi, FhirCreateApi>();
        services.AddSingleton<IFhirUpdateApi, FhirUpdateApi>();
        services.AddSingleton<IFhirDeleteApi, FhirDeleteApi>();
        services.AddSingleton<IFhirTransactionApi, FhirTransactionApi>();

        //Singleton
        services.AddSingleton<IFhirResourceNameSupport, FhirResourceTypeSupport>();

        //Transient
        services.AddTransient<IFhirCallService, FhirCallService.FhirCallService>();
        services.AddTransient<IFhirUriFactory, FhirUriFactory>();
        services.AddTransient<IFhirResourceSearchCache, FhirResourceSearchCache>();
        //services.AddTransient<IFhirNavigator, FhirNavigator>();
        services.AddTransient<IFhirNavigatorFactory, FhirNavigatorFactory>();
    }
}