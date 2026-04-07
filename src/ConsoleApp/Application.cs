using System.Globalization;
using ConsoleApp.CsvMaps;
using ConsoleApp.CsvModels;
using CsvHelper;
using FhirNavigator;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace ConsoleApp;

/// <summary>
/// A simple example that queries for a set of FHIR Patient resources and writes out
/// their FamilyName, GivenName and Gender to a .csv file  
/// </summary>
/// <param name="logger"></param>
/// <param name="appConfig"></param>
/// <param name="fhirNavigatorFactory"></param>
public class Application(
    ILogger<Application> logger,
    IOptions<ApplicationConfiguration> appConfig,
    IFhirNavigatorFactory fhirNavigatorFactory)
{
    private const string RepositoryCode = "Pyro";
    
    public async Task Run()
    {
        logger.LogInformation("Running: {ApplicationName}", appConfig.Value.ApplicationName);
        
        var groupSearchParams = new SearchParams();
        
        groupSearchParams.Add("gender", "female");
        groupSearchParams.Add("_lastUpdated", $"gt2020-01-01");
        groupSearchParams.Add("_sort", "_lastUpdated"); 
        groupSearchParams.Add("_count", "500");
        
        IFhirNavigator fhirNavigator = fhirNavigatorFactory.GetFhirNavigator(RepositoryCode);
        
        SearchInfo searchInfo = await fhirNavigator.Search<Patient>(groupSearchParams);
        
        logger.LogInformation("FHIR Server Name  : {FhirServerName}", fhirNavigator.RepositorySettings.DisplayName);
        logger.LogInformation("FHIR Service Base Url : {ServiceBaseUrl}", fhirNavigator.RepositorySettings.ServiceBaseUrl);
        logger.LogInformation("Total pages on the server : {ResourceTotal}", searchInfo.Pages);
        logger.LogInformation("Total resources returned : {ResourceTotal}", searchInfo.ResourceTotal);
        
        var resourceList = fhirNavigator.Cache.GetList<Patient>();

        //CSV Output file
        var csvOutputFileInfo = GetCsvOutputFileInfo();
        await using var writer = new StreamWriter(csvOutputFileInfo.FullName);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<PatientInfoMap>();
        
        //Output the .csv header row
        csv.WriteHeader<PatientCsvInfo>();
        await csv.NextRecordAsync();
        
        foreach (Patient patient in (resourceList))
        {
            csv.WriteRecord(new PatientCsvInfo()
            {
                Family = patient.Name.FirstOrDefault()?.Family ?? "", 
                GivenName = patient.Name.FirstOrDefault()?.Given.FirstOrDefault() ?? "",
                Gender = patient.Gender?.GetLiteral() ?? ""
            });
            
            await csv.NextRecordAsync();
        }

        await csv.FlushAsync();
        fhirNavigator.Cache.Clear();
        logger.LogInformation("Completed");
    }
    
    private FileInfo GetCsvOutputFileInfo()
    {
        DirectoryInfo csvOutputDirectoryInfo = new DirectoryInfo(appConfig.Value.CsvDirectoryPath);
        csvOutputDirectoryInfo.Create();
        FileInfo csvOutputFileInfo = new FileInfo(Path.Combine(csvOutputDirectoryInfo.FullName, $"patient-info-{DateTime.Now:yyyyMMddHHmmss}.csv"));
        return csvOutputFileInfo;
    }
}

