namespace FhirNavigator.Client
{
  public interface IApiTokenStore
  {
    void AddOrReplaceToken(string key, ApiToken token);
    ApiToken? GetToken(string key);
  }
}