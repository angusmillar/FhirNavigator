﻿using System.Diagnostics.CodeAnalysis;
using FhirNavigator.FhirSupport;
using FhirNavigator.Support;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Sonic.Fhir.Tools.Placer.Application.FhirSupport;

namespace FhirNavigator.FhirUri;

public class FhirUriFactory(
  FhirNavigatorSettings fhirNavigatorSettings,
  IFhirResourceNameSupport fhirResourceNameSupport)
  : IFhirUriFactory
{
  
  private const string MetadataName = "metadata";
  private const string HistoryName = "_history";
  private const string SearchFormDataName = "_search";
  private const string UrnName = "urn";
  private const string OidName = "oid";
  private const string UuidName = "uuid";
  private const string HttpName = "http";
  private const char CanonicalDelimiter = '|';
  private const char UriDelimiter = '/';
  private const char ContainedReferenceToken = '#';

  private const char QueryToken = '?';

  private const char OperationToken = '$';
  
  public FhirUri GetRequired(string repositoryCode, string? resourceReference, string? errorLocationDisplay)
  {
    if (string.IsNullOrWhiteSpace(resourceReference))
    {
      throw new NullReferenceException($"{nameof(resourceReference)}: {errorLocationDisplay ?? string.Empty}");
    }
            
    if (!TryParse(repositoryCode: repositoryCode, requestUri: resourceReference, out FhirUri? fhirUri, out string errorMessage))
    {
      throw new ApplicationException($"{errorLocationDisplay}: resource reference parse failure. {errorMessage}");
    }

    return fhirUri;
  }

  public bool TryParse(string repositoryCode, string requestUri, [NotNullWhen(true)] out  FhirUri? fhirUri, out string errorMessage)
  { 
    FhirRepositorySettings? fhirRepositorySettings = fhirNavigatorSettings.FhirRepositories.FirstOrDefault(x => x.Code.Equals(repositoryCode));
    if (fhirRepositorySettings is null)
    {
      throw new NullReferenceException(nameof(fhirRepositorySettings));
    }
    
    fhirUri = null;
    FhirUri fhirUriParse = new FhirUri(fhirRepositorySettings.ServiceBaseUrl);

    if (ProcessRequestUri(System.Net.WebUtility.UrlDecode(requestUri), fhirUriParse))
    {
      fhirUri = fhirUriParse;
      errorMessage = string.Empty;
      return true;
    }

    errorMessage = fhirUriParse.ParseErrorMessage;
    
    return false;
  }

  private bool ProcessRequestUri(string requestUri, FhirUri fhirUri)
  {
    fhirUri.OriginalString = requestUri;

    string chainResult = ResolveQueryUriPart(requestUri, fhirUri);

    if (fhirUri.ErrorInParsing)
    {
      return false;
    }
    chainResult = ResolvePrimaryServiceRoot(chainResult, fhirUri);

    if (fhirUri.ErrorInParsing)
    {
      return false;
    }
    chainResult = ResolveRelativeUriPart(chainResult, fhirUri);

    if (fhirUri.ErrorInParsing)
    {
      return false;
    }
    chainResult = ResolveResourceIdPart(chainResult, fhirUri);

    if (chainResult != string.Empty)
    {
      fhirUri.ParseErrorMessage = $"The URI has extra unknown content near the end of : '{chainResult}'. The full URI was: '{requestUri}'";
      fhirUri.ErrorInParsing = true;
      return false;
    }

    return true;
  }
  private string ResolveQueryUriPart(string value, FhirUri fhirUri)
  {
    if (value.Contains(QueryToken))
    {
      var split = value.Split(QueryToken);
      fhirUri.Query = split[1];
      return split[0];
    }
    return value;
  }

  private string ResolvePrimaryServiceRoot(string requestUriString, FhirUri fhirUri)
  {

    Uri requestUri = new Uri(requestUriString, UriKind.RelativeOrAbsolute);
    fhirUri.IsAbsoluteUri = false;
    if (!requestUri.IsAbsoluteUri)
    {
      fhirUri.IsAbsoluteUri = true;
      fhirUri.IsRelativeToServer = true;
      return requestUriString;
    }
    
    if (ServiceBaseUrlsMatch())
    {
      fhirUri.IsRelativeToServer = true;
      requestUriString = requestUriString.StripHttp();
      return RemovePrefix(requestUriString, fhirUri.PrimaryServiceRootServers.OriginalString.StripHttp());
    }
   
    if (requestUri.Scheme.StartsWith(HttpName, StringComparison.OrdinalIgnoreCase))
    {
      //If the URL starts with 'http', then it code also be https, so then loop through each segment of the URL looking for 
      //a segment that matches to the FHIR Resource name. Once found we can determine the remote root and return the 
      // relative part.
      fhirUri.IsRelativeToServer = false;
      string pathBuild = string.Empty;
      var pathSplit = requestUri.LocalPath.Split(UriDelimiter);
      for (var i = 0; i < pathSplit.Length; i++)
      {
        if (pathSplit[i].StartsWith(OperationToken) ||
            pathSplit[i].Equals(MetadataName, StringComparison.OrdinalIgnoreCase) ||
            pathSplit[i].Equals(HistoryName, StringComparison.OrdinalIgnoreCase) ||
            fhirResourceNameSupport.IsResourceTypeString(pathSplit[i]))
        {
          //The Service Base Url is found 
          break;
        }

        pathBuild = StringSupport.UrlPathCombine(pathBuild, pathSplit[i]);
      }

      UriBuilder uriBuilder = new UriBuilder(requestUri.Scheme, requestUri.Host, requestUri.Port, pathBuild);
      
      fhirUri.PrimaryServiceRootRemote = new Uri(uriBuilder.Uri.ToString());
      //strip off and set the primary root
      return RemovePrefix( requestUri.OriginalString, fhirUri.PrimaryServiceRootRemote.OriginalString);
    }

    if (requestUri.Scheme.StartsWith(UrnName, StringComparison.OrdinalIgnoreCase))
    {
      fhirUri.IsRelativeToServer = false;
      fhirUri.IsUrn = true;
      if (requestUri.LocalPath.StartsWith($"{UuidName}:", StringComparison.OrdinalIgnoreCase))
      {
        fhirUri.UrnType = UrnType.uuid;
        fhirUri.Urn = requestUri.OriginalString;
        if (!UuidSupport.IsValidValue(fhirUri.Urn))
        {
          fhirUri.ParseErrorMessage = $"The {UrnName}:{UuidName} value given is not valid: {fhirUri.Urn}";
          fhirUri.ErrorInParsing = true;
          return string.Empty;
        }
      }
      if (requestUri.LocalPath.StartsWith($"{OidName}:", StringComparison.OrdinalIgnoreCase))
      {
        fhirUri.UrnType = UrnType.oid;
        fhirUri.Urn = requestUri.OriginalString;
        if (!OidSupport.IsValidValue(fhirUri.Urn))
        {
          fhirUri.ParseErrorMessage = $"The {UrnName}:{OidName} value given is not valid: {fhirUri.Urn}";
          fhirUri.ErrorInParsing = true;
          return string.Empty;
        }
      }
      return RemovePrefix(requestUriString, fhirUri.Urn);
    }

    //The path has not Primary root, it maybe just an Id without a ResourceName, work this out later on       
    return requestUriString;

    bool ServiceBaseUrlsMatch()
    {
      return requestUri.IsAbsoluteUri && requestUri.Authority.Equals(fhirUri.PrimaryServiceRootServers.Authority) && 
             requestUri.LocalPath.StartsWith(fhirUri.PrimaryServiceRootServers.LocalPath, StringComparison.Ordinal);
    }
  }

  private string ResolveRelativeUriPart(string requestRelativePath, FhirUri fhirUri)
  {
    if (requestRelativePath == string.Empty)
      return string.Empty;

    var splitParts = requestRelativePath.Split(QueryToken)[0].Split(UriDelimiter);
    foreach (string segment in splitParts)
    {

      if (segment.StartsWith(OperationToken))
      {
        //It is a base operation          
        fhirUri.OperationType = OperationScope.Base;
        fhirUri.OperationName = segment.TrimStart(OperationToken);
        return requestRelativePath.Substring(fhirUri.OperationName.Length + 1, requestRelativePath.Length - (fhirUri.OperationName.Length + 1));
      }

      if (segment.StartsWith(ContainedReferenceToken))
      {
        //It is a contained reference with out a resource name e.g (#123456) and not (Patient/#123456)
        fhirUri.IsContained = true;
        fhirUri.IsRelativeToServer = false;
        fhirUri.ResourceId = segment.TrimStart(ContainedReferenceToken);
        return requestRelativePath.Substring(fhirUri.ResourceId.Length + 1, requestRelativePath.Length - (fhirUri.ResourceId.Length + 1));
      }

      if (segment.ToLower() == MetadataName)
      {
        //This is a metadata request
        fhirUri.IsMetaData = true;
        return RemovePrefix(requestRelativePath, MetadataName);
      }

      if (segment.ToLower() == HistoryName)
      {
        //This is a metadata request
        fhirUri.IsHistoryReference = true;
        return RemovePrefix(requestRelativePath, HistoryName);
      }

      if (splitParts.Length > 1 || fhirUri.OriginalString.Contains(UriDelimiter))
      {
        //This is a Resource reference where Patient/123456          
        fhirUri.ResourceName = segment;
        if (!fhirResourceNameSupport.IsResourceTypeString(fhirUri.ResourceName))
        {
          fhirUri.ParseErrorMessage = GenerateIncorrectResourceNameMessage(fhirUri.ResourceName);
          fhirUri.ErrorInParsing = true;
          return RemovePrefix(requestRelativePath, fhirUri.ResourceName);
        }
        
        return RemovePrefix(requestRelativePath, fhirUri.ResourceName);
      }

      if (splitParts.Length == 1 && !fhirUri.OriginalString.Contains(UriDelimiter) && fhirResourceNameSupport.IsResourceTypeString(segment))
      {
        fhirUri.ResourceName = segment;
        return string.Empty;
      }

      if (splitParts.Length == 1)
      {
        return segment;
      }
    }
    fhirUri.ParseErrorMessage = $"The URI has no resource or metadata or $Operation or #Contained segment. Found invalid segment: {requestRelativePath} in URL {fhirUri.OriginalString}";
    fhirUri.ErrorInParsing = true;
    return string.Empty;
  }

  private string ResolveResourceIdPart(string value, FhirUri fhirUri)
  {
    string remainder = value;
    if (value == string.Empty)
    {
      return value;
    }
    var split = value.Split(UriDelimiter);
    foreach (string segment in split)
    {
      if (string.IsNullOrWhiteSpace(fhirUri.ResourceId))
      {
        //Resource Id
        if (segment.StartsWith(ContainedReferenceToken))
        {
          //Contained Resource #Id
          fhirUri.IsContained = true;
          fhirUri.IsRelativeToServer = false;
          fhirUri.ResourceId = segment.TrimStart(ContainedReferenceToken);
          remainder = RemoveStartsWithSlash(remainder.Substring(fhirUri.ResourceId.Count() + 1, remainder.Count() - (fhirUri.ResourceId.Count() + 1)));
        }
        else if (segment.ToLower() == SearchFormDataName)
        {
          //Search Form Data 
          fhirUri.IsFormDataSearch = true;
          remainder = RemovePrefix(remainder, SearchFormDataName);
          //Must not be anything after _search, the search parameters are in the body.
          break;
        }
        else if (!fhirUri.IsOperation && segment.StartsWith("$"))
        {
          //A Resource $operation e.g (base/Patient/$operation)              
          fhirUri.OperationType = OperationScope.Resource;
          fhirUri.OperationName = segment.TrimStart(OperationToken);
          remainder = RemoveStartsWithSlash(remainder.Substring(fhirUri.OperationName.Count() + 1, remainder.Count() - (fhirUri.OperationName.Count() + 1)));
          return remainder;
        }
        else
        {
          //Could have a Canonical version e.g CodeSystem/myCodeSystem|2.0
          if (segment.Contains(CanonicalDelimiter))
          {
            string[] splitCanonical = segment.Split(CanonicalDelimiter);
            fhirUri.ResourceId = splitCanonical[0];
            fhirUri.CanonicalVersionId = splitCanonical[1];
            int totalLength = fhirUri.ResourceId.Length + fhirUri.CanonicalVersionId.Length + 1;
            remainder = RemoveStartsWithSlash(remainder.Substring(totalLength, remainder.Length - totalLength));
          }
          else
          {
            //Normal Resource Id
            fhirUri.ResourceId = segment;
            remainder = RemovePrefix(remainder, fhirUri.ResourceId);
          }

          //If its only a resourceId or its only a IsContained resource Id (e.g. #100 or 100) then nether are RelativeToServer
          if (string.IsNullOrEmpty(fhirUri.ResourceName) || fhirUri.IsContained)
          {
            fhirUri.IsRelativeToServer = false;
          }
          
        }
      }
      else
      {

        if (!fhirUri.IsOperation && !string.IsNullOrWhiteSpace(fhirUri.ResourceId) && segment.StartsWith(OperationToken))
        {
          //A Resource Instance $operation e.g (base/Patient/10/$operation)              
          fhirUri.OperationType = OperationScope.Instance;
          fhirUri.OperationName = segment.TrimStart(OperationToken);
          remainder = RemoveStartsWithSlash(remainder.Substring(fhirUri.OperationName.Length + 1, remainder.Length - (fhirUri.OperationName.Length + 1)));
          return remainder;
        }

        if (segment.ToLower() == HistoryName)
        {
          //History segment e.g (_history)
          //Is this case iterate over loop again to see is we have a Resource VersionId
          fhirUri.IsHistoryReference = true;
          remainder = RemovePrefix(remainder, HistoryName);
        }
        else if (fhirUri.IsHistoryReference)
        {
          //History version id
          fhirUri.VersionId = segment;
          remainder = RemovePrefix(remainder, fhirUri.VersionId);
          return remainder;
        }
        else if (fhirResourceNameSupport.IsResourceTypeString(segment))
        {
          //Is this a Compartment reference e.g ([base]/Patient/[id]/Condition?code:in=http://hspc.org/ValueSet/acute-concerns)
          //where 'Patient' is the compartment and 'Condition' is the resource.
          fhirUri.CompartmentalisedResourceName = segment;
          fhirUri.IsCompartment = true;
          remainder = RemovePrefix(remainder, fhirUri.CompartmentalisedResourceName);
          return remainder;
        }
      }
    }
    return remainder;
  }

  private string RemovePrefix(string input, string prefixToRemove)
  {
    return RemoveStartsWithSlash(input.Substring(prefixToRemove.Length, input.Length - prefixToRemove.Length));
  }

  private static string RemoveStartsWithSlash(string value)
  {
    if (value.StartsWith(UriDelimiter))
    {
      value = value.Substring(1, value.Length -1 );
    }
    return value;
  }
  
  private string GenerateIncorrectResourceNameMessage(string resourceName)
  {
    if (resourceName.ToLower() == "conformance")
    {
      return $"The resource name '{resourceName}' is not supported by FHIR Version: {ModelInfo.Version}. Perhaps you wish to find the server's conformance statement resource named 'CapabilityStatement' which can be obtained from the endpoint '[base]/metadata' ";
    }

    if (char.IsLower(resourceName.ToCharArray()[0]))
    {
      if (fhirResourceNameSupport.IsResourceTypeString(StringSupport.UppercaseFirst(resourceName)))
      {
        return $"The resource name or Compartment name '{resourceName}' must begin with a capital letter, e.g ({StringSupport.UppercaseFirst(resourceName)})";
      }
      return $"The resource name or Compartment name given '{resourceName}' is not a Resource supported by the FHIR Version: {ModelInfo.Version}.";
    }
    return $"The resource name or compartment is not supported for this FHIR version. The resource name found in the URI was {resourceName} and the version of FHIR used was {ModelInfo.Version}. Remember that FHIR resource names are case sensitive.";
  }
  
}
