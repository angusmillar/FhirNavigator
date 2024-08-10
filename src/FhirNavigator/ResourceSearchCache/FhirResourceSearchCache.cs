using Hl7.Fhir.Model;

namespace FhirNavigator.ResourceSearchCache;

public class FhirResourceSearchCache : IFhirResourceSearchCache
{
    private readonly Dictionary<string, Dictionary<string, Resource>> Dictionary = new();

    public void Add(IEnumerable<Resource> resourceList)
    {
        foreach (Resource resource in resourceList)
        {
            Add(resource);
        }
    }

    public int ResourceCount()
    {
        int count = 0;
        foreach (var resourceDictionaryValue in Dictionary.Values)
        {
            count = count + resourceDictionaryValue.Count;
        }
        return count;
    }
    
    public int ResourceCount<T>() where T : Resource
    {
        string resourceName = typeof(T).Name;
        if (Dictionary.TryGetValue(resourceName, out var value))
        {
            return value.Count;
        }

        return 0;
    }

    public void Add(Bundle bundle)
    {
        bundle.Entry.ForEach(x => Add(x.Resource));
    }

    public void Add(IEnumerable<Bundle> bundleList)
    {
        foreach (Bundle bundle in bundleList)
        {
            Add(bundle);
        }
    }

    public void Add(Resource resource)
    {
        Add(resource.Id, resource);
    }

    public T? Get<T>(string resourceId) where T : Resource
    {
        string resourceName = typeof(T).Name;
        if (Dictionary.ContainsKey(resourceName))
        {
            if (Dictionary[resourceName].ContainsKey(resourceId))
            {
                if (Dictionary[resourceName][resourceId] is T result)
                {
                    return result;
                }
            }
        }

        return null;
    }

    public List<T> GetList<T>() where T : Resource
    {
        return GetResourceDictionary<T>().Select(x => x.Value).ToList();
    }

    public bool ContainsKey<T>(string resourceId) where T : Resource
    {
        string resourceName = typeof(T).Name;
        if (Dictionary.TryGetValue(resourceName, out var value))
        {
            if (value.ContainsKey(resourceId))
            {
                return true;
            }
        }

        return false;
    }

    public bool Remove<T>(string resourceId) where T : Resource
    {
        string resourceName = typeof(T).Name;
        if (Dictionary.TryGetValue(resourceName, out var value))
        {
            return value.Remove(resourceId);
        }

        return false;
    }

    public Dictionary<string, T> GetResourceDictionary<T>() where T : Resource
    {
        var result = new Dictionary<string, T>();
        string resourceName = typeof(T).Name;
        if (Dictionary.TryGetValue(resourceName, out var value))
        {
            foreach (Resource resource in value.Values)
            {
                if (resource is T typedResource)
                {
                    result.Add(typedResource.Id, typedResource);
                }
            }
            return result;
        }
        
        return result;
        
    }

    public void Clear()
    {
        Dictionary.Clear();
    }

    public void Clear<T>() where T : Resource
    {
        string resourceName = typeof(T).Name;
        if (Dictionary.TryGetValue(resourceName, out var value))
        {
            value.Clear();
        }
    }

    private void Add(string resourceId,
        Resource resource)
    {
        string resourceName = resource.GetType().Name;
        if (!Dictionary.ContainsKey(resourceName))
        {
            Dictionary.Add(resourceName, new Dictionary<string, Resource>());
        }

        if (!Dictionary[resourceName].ContainsKey(resourceId))
        {
            Dictionary[resourceName].Add(resourceId, resource);
        }
        else
        {
            Dictionary[resourceName][resourceId] = resource;
        }
    }
}