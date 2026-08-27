using Hl7.Fhir.Model;

namespace FhirNavigator.FhirSupport;

public class FhirResourceTypeSupport : IFhirResourceNameSupport
{
  private readonly string[] FhirResourceTypeHashSet = ModelInfo.SupportedResources.ToArray();
  
  public bool IsResourceTypeString(string value)
  {
    return FhirResourceTypeHashSet.Contains(value);
  }
  
}
