using FhirNavigator.Proxy;

namespace FhirNavigator;

public class FhirNavigatorSettings
{
    public const string SectionName = "FhirNavigator";

    public required List<FhirRepositorySettings> FhirRepositories { get; set; }

    public required ProxySettings? Proxy { get; set; }

    public static FhirNavigatorSettings GetDefault()
    {
        return new FhirNavigatorSettings()
        {
            FhirRepositories = new List<FhirRepositorySettings>(),
            Proxy = null,
        };
    }
}