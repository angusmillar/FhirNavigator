using FhirNavigator.FhirCallService;
using FhirNavigator.ResourceSearchCache;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Sonic.Fhir.Tools.Placer.Application.FhirSupport;
using Task = System.Threading.Tasks.Task;

namespace FhirNavigator;

public class FhirNavigator(
    IFhirCallService fhirCallService,
    IFhirUriFactory fhirUriFactory,
    string repositoryCode) : IFhirNavigator
{
    
    public IFhirResourceSearchCache Cache => fhirCallService.Cache;
    
    public async Task<SearchInfo> Search<T>(SearchParams searchParams, int? pageLimiter = null) where T : Resource
    {
         return await fhirCallService.Search<T>(
            repositoryCode: repositoryCode, 
            searchParameter: searchParams, 
            pageLimiter: pageLimiter);
    }
    
    public async Task<T> UpdateResource<T>(T resource, bool versionAware = true) where T : Resource
    {
        T? fhirResource = await fhirCallService.Update(repositoryCode: repositoryCode, resource: resource, versionAware: versionAware);
        if (fhirResource is null)
        {
            throw new ApplicationException($"Could not update {typeof(T).Name} FHIR resource for reference : {GetResourceTypeName<T>()}/{resource.Id}");
        }

        return fhirResource;
    }

    public async Task<T> CreateResource<T>(T resource) where T : Resource
    {
        T? fhirResource = await fhirCallService.Create(repositoryCode: repositoryCode, resource: resource);
        if (fhirResource is null)
        {
            throw new ApplicationException($"Could not create {typeof(T).Name} FHIR resource for reference : {GetResourceTypeName<T>()}/{resource.Id}");
        }

        return fhirResource;
    }

    public async Task DeleteResource<T>(string resourceId) where T : Resource
    {
        await fhirCallService.Delete<T>(repositoryCode: repositoryCode, resourceId: resourceId);
    }

    public async Task<Bundle> Transaction(Bundle transactionBundle)
    {
        return await fhirCallService.Transaction(repositoryCode: repositoryCode, transactionBundle: transactionBundle);
    }
    
    public async Task<T?> GetResource<T>(string resourceId) where T : Resource
    {
        T? fhirResource = Cache.Get<T>(resourceId);

        if (fhirResource is null)
        {
            fhirResource = await fhirCallService.GetById<T>(repositoryCode: repositoryCode, resourceId);
        }
        
        return fhirResource;
    }

    public async Task<T?> GetResource<T>(ResourceReference? resourceReference,
        string? errorLocationDisplay,
        Resource parentResource) where T : Resource
    {
        return await GetResourceAll<T>(resourceReference, errorLocationDisplay, parentResource);
    }

    public async Task<T?> GetResource<T>(ResourceReference? resourceReference,
        string? errorLocationDisplay) where T : Resource
    {
        return await GetResourceAll<T>(resourceReference, errorLocationDisplay, parentResource: null);
    }

    private async Task<T?> GetResourceAll<T>(
        ResourceReference? resourceReference,
        string? errorLocationDisplay,
        Resource? parentResource = null) where T : Resource
    {
        if (string.IsNullOrWhiteSpace(resourceReference?.Reference))
        {
            throw new NullReferenceException($"{nameof(resourceReference)} was null. {errorLocationDisplay ?? string.Empty}");
        }

        FhirUri.FhirUri parsedResourceReference = fhirUriFactory.GetRequired(repositoryCode: repositoryCode, resourceReference: resourceReference.Reference, errorLocationDisplay: errorLocationDisplay);

        if (string.IsNullOrWhiteSpace(parsedResourceReference.ResourceId))
        {
            throw new NullReferenceException($"Unable to locate resource id in the {nameof(resourceReference)} of {resourceReference.Reference}. {errorLocationDisplay ?? string.Empty}");
        }

        if (!parsedResourceReference.ResourceName.Equals(typeof(T).Name))
        {
            throw new ApplicationException(
                $"The target resource type of {parsedResourceReference.ResourceName} does not align with the requested type of {typeof(T).Name} for the full reference of {resourceReference.Reference}. {errorLocationDisplay ?? string.Empty}");
        }

        if (parsedResourceReference.IsContained)
        {
            if (parentResource is null)
            {
                throw new NullReferenceException($"The target Resource Reference is of type Contained however the parent resource provided was null. Reference was: {resourceReference.Reference}. ");
            }

            if (parentResource is not DomainResource  parentDomainResource)
            {
                throw new ApplicationException(
                    $"The target Resource Reference is of type Contained however the parent resource has no is only a DomainResource type which can no hold Contained resources. Reference was: {resourceReference.Reference}. ");
            }
            
            if (parentDomainResource.Contained is null)
            {
                throw new ApplicationException(
                    $"The target Resource Reference is of type Contained however the parent resource has no Contained references. Reference was: {resourceReference.Reference}. ");
            }

            Resource? containedResource = parentDomainResource.Contained.FirstOrDefault(x => x.Id.Equals(parsedResourceReference.ResourceId));
            if (containedResource is null)
            {
                // throw new ApplicationException(
                //     $"The target Resource Reference is of type Contained however its resource is not found in the  parent resource's Contained element. Reference was: {resourceReference.Reference}. ");
                return null;
            }

            if (containedResource is not T typedContainedResource)
            {
                throw new ApplicationException(
                    $"The target Resource Reference is of type Contained however its matching Contained resource in of type {containedResource.TypeName} and not the requested type of {typeof(T)}. Reference was: {resourceReference.Reference}. ");
            }

            return typedContainedResource;
        }

        T? fhirResource = Cache.Get<T>(parsedResourceReference.ResourceId);

        if (fhirResource is null)
        {
            return await fhirCallService.GetById<T>(repositoryCode: repositoryCode, parsedResourceReference.ResourceId);
        }

        return fhirResource;
    }

    private static string? GetResourceTypeName<T>() where T : Resource
    {
        string? resourceTypeName = Hl7.Fhir.Model.ModelInfo.GetFhirTypeNameForType(typeof(T));
        if (resourceTypeName is null)
        {
            throw new NullReferenceException(nameof(resourceTypeName));
        }

        return resourceTypeName;
    }
}