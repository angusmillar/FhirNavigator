using FhirNavigator.Exceptions;
using Hl7.Fhir.Model;

namespace FhirNavigator.Api;

public abstract class FhirApiBase
{
    protected static void ThrowIfRepositoryCodeEmptyString(string repositoryCode)
    {
        if (string.IsNullOrWhiteSpace(repositoryCode))
        {
            throw new FhirNavigatorException($"{nameof(repositoryCode)} must not be an empty string");
        }
    }

    protected static string GetResourceTypeName<T>() where T : Resource
    {
        string? resourceTypeName = ModelInfo.GetFhirTypeNameForType(typeof(T));
        if (resourceTypeName is null || !ModelInfo.IsKnownResource(resourceTypeName))
        {
            throw new FhirNavigatorException($"Type must be a known FHIR Resource Type");
        }

        return resourceTypeName;
    }
}