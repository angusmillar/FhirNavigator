using FhirNavigator.Api;
using FhirNavigator.ResourceSearchCache;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FhirNavigator.FhirCallService;

public class FhirCallService(
    IFhirGetApi fhirGetApi,
    IFhirSearchApi fhirSearchApi,
    IFhirCreateApi fhirCreateApi,
    IFhirUpdateApi fhirUpdateApi,
    IFhirDeleteApi fhirDeleteApi,
    IFhirTransactionApi fhirTransactionApi,
    IFhirResourceSearchCache fhirResourceSearchCache) : IFhirCallService
{
    public IFhirResourceSearchCache Cache => fhirResourceSearchCache;

    public async Task<T?> Update<T>(string repositoryCode,
        T resource, bool versionAware = true) where T : Resource
    {
        return await fhirUpdateApi.UpdateAsync<T>(repositoryCode: repositoryCode, resource, versionAware: versionAware);
    }

    public async Task<T> Create<T>(string repositoryCode,
        T resource) where T : Resource
    {
        return await fhirCreateApi.CreateAsync<T>(repositoryCode: repositoryCode, resource);
    }

    public async Task Delete<T>(string repositoryCode,
        string resourceId) where T : Resource
    {
        await fhirDeleteApi.DeleteAsync<T>(repositoryCode: repositoryCode, id: resourceId);
    }

    public async Task<T?> GetById<T>(string repositoryCode,
        string resourceId) where T : Resource
    {
        return await fhirGetApi.GetAsync<T>(repositoryCode: repositoryCode, resourceId);
    }

    public async Task<SearchInfo> Search<T>(string repositoryCode,
        SearchParams searchParameter,
        int? pageLimiter) where T : Resource
    {
        int pageCounter = 0;
        int resourceCounter = 0;
        Bundle? bundle = null;

        SearchInfo? searchMetaData = GetDefaultSearchMetaData(pageLimiter: pageLimiter);
        pageLimiter ??= int.MaxValue;

        while (bundle is not null && pageCounter < pageLimiter || pageCounter == 0)
        {
            if (pageCounter == 0)
            {
                bundle = await fhirSearchApi.GetAsync<T>(
                    repositoryCode: repositoryCode,
                    searchParameter);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(bundle);

                //Returns null if there is no next page
                bundle = await fhirSearchApi.ContinueAsync(
                    repositoryCode: repositoryCode,
                    previousBundle: bundle);
            }

            pageCounter++;

            if (bundle is not null)
            {
                resourceCounter += bundle.Entry.Count;
                searchMetaData = GetSearchMetaData(bundle, pageLimiter: pageLimiter, resourceCounter: resourceCounter,
                    pageCounter: pageCounter);
                fhirResourceSearchCache.Add(bundle);
            }
        }

        return searchMetaData;
    }

    private SearchInfo GetSearchMetaData(Bundle bundle, int? pageLimiter, int resourceCounter, int pageCounter)
    {
        return new SearchInfo(
            BundleTotal: bundle.Total,
            ResourceTotal: resourceCounter,
            HasNextPage: (bundle.NextLink is not null),
            Pages: pageCounter,
            PageLimiter: pageLimiter == int.MaxValue ? null : pageLimiter,
            NextLink: bundle.NextLink,
            FistLink: bundle.FirstLink,
            LastLink: bundle.LastLink,
            PreviousLink: bundle.PreviousLink,
            SelfLink: bundle.SelfLink,
            Identifier: bundle.Identifier,
            TimeStamp: bundle.Timestamp,
            Signature: bundle.Signature);
    }

    private SearchInfo GetDefaultSearchMetaData(int? pageLimiter)
    {
        return new SearchInfo(
            BundleTotal: null,
            ResourceTotal: 0,
            Pages: 0,
            PageLimiter: pageLimiter,
            HasNextPage: false,
            NextLink: null,
            FistLink: null,
            LastLink: null,
            PreviousLink: null,
            SelfLink: null,
            Identifier: null,
            TimeStamp: null,
            Signature: null);
    }

    public async Task<Bundle> Transaction(string repositoryCode, Bundle transactionBundle)
    {
        if (transactionBundle.Type is null)
        {
            throw new ApplicationException("Bundle.type must be of type Transaction and not null. ");
        }

        if (!transactionBundle.Type.Equals(Bundle.BundleType.Transaction))
        {
            throw new ApplicationException(
                $"Bundle.type must be of type Transaction, found {transactionBundle.Type.ToString()}. ");
        }

        return await fhirTransactionApi.CommitAsync(repositoryCode: repositoryCode, transactionBundle);
    }
}