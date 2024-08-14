using FhirNavigator.ResourceSearchCache;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace FhirNavigator.FhirCallService;

public interface IFhirCallService
{
    IFhirResourceSearchCache Cache { get; } 
    /// <summary>
    /// Will attempt to get the resource from the FHIR repository, returns null if not found. 
    /// </summary>
    /// <param name="repositoryCode"></param>
    /// <param name="resourceId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> GetById<T>(string repositoryCode, string resourceId) where T : Resource;

    /// <summary>
    /// Will perform a version aware resource update against the FHIR repository
    /// </summary>
    /// <param name="repositoryCode"></param>
    /// <param name="resource"></param>
    /// <param name="versionAware"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> Update<T>(string repositoryCode, T resource, bool versionAware = true) where T : Resource;


    /// <summary>
    /// Will attempt to create the resource from the FHIR repository. 
    /// </summary>
    /// <param name="repositoryCode"></param>
    /// <param name="resource"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> Create<T>(string repositoryCode,
        T resource) where T : Resource;
    
    /// <summary>
    /// Will attempt to delete the resource from the FHIR repository. 
    /// </summary>
    /// <param name="repositoryCode"></param>
    /// <param name="resourceId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task Delete<T>(string repositoryCode,
        string resourceId) where T : Resource;
    
    /// <summary>
    /// Will run the 'searchParameter' query against the FHIR repository.   
    /// The 'pageLimiter' value will limit how many returned pages the call will iterate before returning the set.
    /// If 'pageLimiter' is zero or less, which is the default, it will attempt to return the entire set.    
    /// Care should be given when using pageLimiter = 0 as this may consume all application memory should
    /// the search return a very large set of page.  
    /// </summary>
    /// <param name="repositoryCode"></param>
    /// <param name="searchParameter"></param>
    /// <param name="pageLimiter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<SearchInfo> Search<T>(string repositoryCode, Hl7.Fhir.Rest.SearchParams searchParameter,
        int? pageLimiter = null) where T : Resource;

    /// <summary>
    /// Will attempt to POST an transaction bundle to the base of the FHIR repository. 
    /// </summary>
    /// <param name="repositoryCode"></param>
    /// <param name="transactionBundle"></param>
    /// <returns></returns>
    Task<Bundle> Transaction(string repositoryCode,
        Bundle transactionBundle);

}