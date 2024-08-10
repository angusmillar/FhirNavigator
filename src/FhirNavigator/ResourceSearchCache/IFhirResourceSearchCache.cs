using Hl7.Fhir.Model;

namespace FhirNavigator.ResourceSearchCache;

public interface IFhirResourceSearchCache
{
    void Add(IEnumerable<Resource> resourceList);
    void Add(Bundle bundle);
    void Add(IEnumerable<Bundle> bundleList);
    void Add(Resource resource);
    int ResourceCount();
    int ResourceCount<T>() where T : Resource;
    T? Get<T>(string resourceId) where T : Resource;
    List<T> GetList<T>() where T : Resource;
    bool ContainsKey<T>(string resourceId) where T : Resource;
    bool Remove<T>(string resourceId) where T : Resource;
    Dictionary<string, T> GetResourceDictionary<T>() where T : Resource;
    void Clear();
    void Clear<T>() where T : Resource;
}