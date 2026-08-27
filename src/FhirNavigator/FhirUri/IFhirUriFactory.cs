using System.Diagnostics.CodeAnalysis;

namespace FhirNavigator.FhirUri;

public interface IFhirUriFactory
{
  bool TryParse(string repositoryCode, string requestUri, [NotNullWhen(true)] out FhirUri? fhirUri, out string errorMessage);

  FhirUri GetRequired(string repositoryCode, string? resourceReference, string? errorLocationDisplay);

}
