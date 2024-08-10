using FhirNavigator.Client.Handlers;

namespace FhirNavigator.Factories;

public class AuthenticationDelegatingHandlerFactory(IServiceProvider serviceProvider) : IAuthenticationDelegatingHandlerFactory
{
  public AuthenticationDelegatingHandler Get(FhirRepositorySettings fhirRepositorySettings)
  {
    object? obj = serviceProvider.GetService(typeof(AuthenticationDelegatingHandler));
    if (obj is not null && obj is AuthenticationDelegatingHandler authenticationDelegatingHandler)
    {
      authenticationDelegatingHandler.OrderRepositorySettings = fhirRepositorySettings;
      return authenticationDelegatingHandler;
    }
    throw new InvalidOperationException($"'{nameof(authenticationDelegatingHandler)}' was unable to resolve from the IServiceProvider.");
  }
}