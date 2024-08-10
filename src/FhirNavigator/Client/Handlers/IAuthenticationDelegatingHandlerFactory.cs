namespace FhirNavigator.Client.Handlers;

public interface IAuthenticationDelegatingHandlerFactory
{
  AuthenticationDelegatingHandler Get(FhirRepositorySettings fhirRepositorySettings);
}