using FhirNavigator.ResourceSearchCache;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirNavigator;

public interface IFhirNavigator
{
    
    IFhirResourceSearchCache Cache { get; }
    
    System.Threading.Tasks.Task<SearchInfo> Search<T>(SearchParams searchParams,
        int? pageLimiter = null) where T : Resource;
    
    /// <summary>
    /// Will get the FHIR resource that is referenced by resourceReference.
    /// If a contained reference, then throw an error
    /// Otherwise, attempt to load the referenced resource from the FHIR  Repository via a FHIR GET API call  
    /// </summary>
    /// <param name="resourceReference"></param>
    /// <param name="errorLocationDisplay"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> GetResource<T>(
        ResourceReference? resourceReference,
        string? errorLocationDisplay) where T : Resource;

    /// <summary>
    /// Will get the FHIR resource that is referenced by resourceReference.
    /// If a contained reference, then get the resource for that parentResource's contained section
    /// Otherwise, attempt to load the referenced resource from the FHIR  Repository via a FHIR GET API call  
    /// </summary>
    /// <param name="resourceReference"></param>
    /// <param name="errorLocationDisplay"></param>
    /// <param name="parentResource"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> GetResource<T>(
        ResourceReference? resourceReference,
        string? errorLocationDisplay,
        Resource parentResource) where T : Resource;
    
    Task<T?> GetResource<T>(string resourceId) where T : Resource;
    
    Task<T> UpdateResource<T>(T resource, bool versionAware = true) where T : Resource;
    
    Task<T> CreateResource<T>(T resource) where T : Resource;
    
    System.Threading.Tasks.Task DeleteResource<T>(string resourceId) where T : Resource;
    
    Task<Bundle> Transaction(Bundle transactionBundle);
    
}