namespace FhirNavigator
{
  /// <summary>
  /// Setting for an external Order Repository
  /// </summary>
  public class FhirRepositorySettings
  {
    public const string SectionName = "FhirNavigatorRepositories";
    /// <summary>
    /// A unique internal Code for this external Order Repository
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// A human friendly display name for this Order repository
    /// </summary>
    public required string DisplayName { get; init; } = string.Empty;
    
    /// <summary>
    /// The base address to the external Order Repository FHIR server endpoint
    /// Should not end is a '/', (e.g. https://somewhere.com.au/fhir)
    /// </summary>
    public required Uri ServiceBaseUrl { get; init; }

    /// <summary>
    /// Weather to use OAuth 2.0 client credentials flow
    /// If true you will also need to provide the TokenEndpointUrl, ClientId, ClientSecret and any required Scopes. 
    /// </summary>
    public required bool UseOAuth2 { get; init; } = false;
    /// <summary>
    /// A unique internal Code for this external Order Repository
    /// </summary>
    public string OAuth2ClientCode => $"{Code}-OAuth2";

    /// <summary>
    /// The OAuth2 Token Endpoint
    /// </summary>
    public Uri TokenEndpointUrl { get; set; } = new Uri("http://nowhere");

    /// <summary>
    /// The OAuth2 ClientId
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth2 Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth2 Scopes
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// A specify API Key to be added as a header, not add if the value is empty string
    /// </summary>
    public string X_API_Key { get; set; } = string.Empty;

    /// <summary>
    /// Weather to use Basic Auth with the following Username and Password
    /// </summary>
    public required bool UseBasicAuth { get; init; } = false;
    
    /// <summary>
    /// Basic Auth username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Basic Auth password
    /// </summary>
    public string Password { get; set; } = string.Empty;

  }
}
