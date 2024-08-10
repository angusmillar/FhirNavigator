using FhirNavigator.Exceptions;
using FhirNavigator.FhirHttpClient;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace FhirNavigator.Api;

public class FhirSearchApi(
    IFhirHttpClientFactory fhirHttpClientFactory,
    ILogger<FhirSearchApi> logger) : FhirApiBase, IFhirSearchApi
{
    public async Task<Bundle?> GetAsync<T>(string repositoryCode,
        SearchParams fhirQuery) where T : Resource
    {
        string resourceName = GetResourceTypeName<T>();
        return await SendQuery(repositoryCode, resourceName, fhirQuery: fhirQuery, previousBundle: null);
    }

    public async Task<Bundle?> ContinueAsync(string repositoryCode,
        Bundle previousBundle)
    {
        return await SendQuery(repositoryCode, resourceName: null, fhirQuery: null, previousBundle);
    }

    private async Task<Bundle?> SendQuery(string repositoryCode,
        string? resourceName = null,
        SearchParams? fhirQuery = null,
        Bundle? previousBundle = null)
    {
        ThrowIfRepositoryCodeEmptyString(repositoryCode);
        LogSearchQuery(repositoryCode, resourceName, fhirQuery, previousBundle);
        FhirClient fhirClient = fhirHttpClientFactory.CreateClient(repositoryCode);
        try
        {
            if (fhirQuery is not null && previousBundle is null)
            {
                return await fhirClient.SearchAsync(fhirQuery, resourceName);
            }

            if (fhirQuery is null && previousBundle is not null)
            {
                return await fhirClient.ContinueAsync(previousBundle);
            }

            throw new FhirNavigatorException($"Only one of {nameof(fhirQuery)} or {nameof(previousBundle)} can be provided at once.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "{ExceptionType} when querying external FHIR endpoint for RepositoryCode: {OrderRepositoryCode}, {Query}, ErrorMessage: {ExceptionMessage}",
                exception.GetType().Name,
                repositoryCode,
                GetQueryForLogging(resourceName, fhirQuery: fhirQuery, previousBundle: previousBundle),
                exception.Message);
            throw;
        }
    }

    private void LogSearchQuery(string repositoryCode,
        string? resourceName,
        SearchParams? fhirQuery,
        Bundle? previousBundle)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            string query = GetQueryForLogging(resourceName, fhirQuery, previousBundle);
            logger.LogDebug("FHIR search query for Repository Code : {RepositoryCode} is : {Query}", repositoryCode, query);
        }
    }
    
    private static string GetQueryForLogging(string? resourceName,
        SearchParams? fhirQuery,
        Bundle? previousBundle)
    {
        if (fhirQuery is not null && !string.IsNullOrWhiteSpace(resourceName))
        {
            return $"GET [base]/{resourceName}?{fhirQuery.ToUriParamList()}";
        }

        if (previousBundle is not null)
        {
            Bundle.LinkComponent? nextLink = previousBundle.Link.FirstOrDefault(x => x.Relation.Equals("next", StringComparison.OrdinalIgnoreCase));
            if (nextLink is not null)
            {
                return $"GET {nextLink.Url}";
            }
        }

        return $"GET [base]/?[Unknown search]";
    }
}