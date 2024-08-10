namespace FhirNavigator.Proxy
{
  public class ProxySettings
  {
    public const string SectionName = "Proxy";
    public required bool UseProxy { get; init; }
    public required string ProxyUsername { get; init; } 
    public required string ProxyPassword { get; init; } 
    public required string ProxyDomain { get; init; } 
    public required string ProxyHostAddress { get; init; } 
    public required int ProxyHostPort { get; init; }
  }
}
