using Hl7.Fhir.Model;
using Sonic.Fhir.Tools.Placer.Application.FhirSupport;

namespace FhirNavigator.FhirSupport;

public class FhirResourceTypeSupport : IFhirResourceNameSupport
{
  private readonly string[] FhirResourceTypeHashSet = ModelInfo.SupportedResources.ToArray();
  
  public bool IsResourceTypeString(string value)
  {
    return FhirResourceTypeHashSet.Contains(value);
  }
  
}
