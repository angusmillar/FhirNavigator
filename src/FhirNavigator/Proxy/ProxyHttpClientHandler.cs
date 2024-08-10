using System.Net;

namespace FhirNavigator.Proxy
{
  public class ProxyHttpClientHandler : HttpClientHandler
  {
    public ProxyHttpClientHandler(FhirNavigatorSettings fhirNavigatorSettings)
    {
      
      if (fhirNavigatorSettings.Proxy is null || !fhirNavigatorSettings.Proxy.UseProxy)
      {
        DefaultProxyCredentials = CredentialCache.DefaultNetworkCredentials;
        return;
      }
      
      NetworkCredential? proxyCredential = null;
      if (!string.IsNullOrWhiteSpace(fhirNavigatorSettings.Proxy.ProxyUsername) && !string.IsNullOrWhiteSpace(fhirNavigatorSettings.Proxy.ProxyPassword))
      {
        if (string.IsNullOrWhiteSpace(fhirNavigatorSettings.Proxy.ProxyDomain))
        {
          proxyCredential = new NetworkCredential(fhirNavigatorSettings.Proxy.ProxyUsername, fhirNavigatorSettings.Proxy.ProxyPassword, fhirNavigatorSettings.Proxy.ProxyDomain);
        }
        else
        {
          proxyCredential = new NetworkCredential(fhirNavigatorSettings.Proxy.ProxyUsername, fhirNavigatorSettings.Proxy.ProxyPassword);
        }
      }
      
      string proxyAddressUriString = $"{fhirNavigatorSettings.Proxy.ProxyHostAddress}:{fhirNavigatorSettings.Proxy.ProxyHostPort}";
      if (Uri.TryCreate(proxyAddressUriString, UriKind.Absolute, out Uri? proxyUri))
      {
        UseProxy = true;
        Proxy = new WebProxy
        {
          Address = proxyUri,
          BypassProxyOnLocal = false,
          UseDefaultCredentials = false,
          Credentials = proxyCredential
        };
      }
      else
      {
        throw new ApplicationException($"The FhirNavigator ProxySettings of ProxyHostAddress and ProxyHostPort do not combine to make a valid Uri, value was : {proxyAddressUriString}");
      }
    }
  }
}
