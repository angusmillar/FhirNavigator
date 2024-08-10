using Hl7.Fhir.Model;

namespace FhirNavigator;

public record SearchInfo(
    int? BundleTotal, 
    int ResourceTotal,
    bool HasNextPage, 
    int Pages,
    int? PageLimiter,
    Uri? NextLink, 
    Uri? FistLink, 
    Uri? LastLink, 
    Uri? PreviousLink, 
    Uri? SelfLink,
    Identifier? Identifier,
    DateTimeOffset? TimeStamp,
    Signature? Signature);