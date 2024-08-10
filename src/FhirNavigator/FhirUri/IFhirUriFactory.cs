using System.Diagnostics.CodeAnalysis;
using FhirNavigator.FhirUri;

namespace Sonic.Fhir.Tools.Placer.Application.FhirSupport;

public interface IFhirUriFactory
{
  bool TryParse(string repositoryCode, string requestUri, [NotNullWhen(true)] out FhirUri? fhirUri, out string errorMessage);

  FhirUri GetRequired(string repositoryCode, string? resourceReference, string? errorLocationDisplay);

}
