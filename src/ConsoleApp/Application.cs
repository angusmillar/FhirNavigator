using System.Globalization;
using ConsoleApp.CsvMaps;
using ConsoleApp.CsvModels;
using CsvHelper;
using FhirNavigator;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace ConsoleApp;

public class Application(
    ILogger<Application> logger,
    IOptions<ApplicationConfiguration> appConfig,
    IFhirNavigatorFactory fhirNavigatorFactory)
{
    private const string RepositoryCode = "GenieTest";
    
    public async Task Run()
    {
        logger.LogInformation("Running: {ApplicationName}", appConfig.Value.ApplicationName);
        
        var groupSearchParams = new SearchParams();
        
        //We do not want returned parent Tasks which are canceled and already marked as handled by us  
        string cancelHandledCode = $"cancel-handled";
        
        //We do not want returned new parent Tasks which are involved in a request claiming operation   
        string claimedRequestCode = $"claimed-request";

        //Each Task has a parent Grouper Task with Children Tasks, here we only want to source the parent grouper tasks
        string groupTaskTag = $"http://fhir.geniesolutions.io/CodeSystem/eorders-tag|fulfillment-task-group";

        //HPI-O Identifier
        string hpioSearchToken = $"http://ns.electronichealth.net.au/id/hi/hpio/1.0|8003622500042859"; //DHM HPI-O
        
        //groupSearchParams.Add("_id", "ce261e6b-6d67-546f-98f5-0aecfcc5b42e");
            
        groupSearchParams.Add("status", "in-progress");
        groupSearchParams.Add("business-status:not", cancelHandledCode);
        groupSearchParams.Add("business-status:not", claimedRequestCode);
        groupSearchParams.Add("_tag", groupTaskTag);
        groupSearchParams.Add("_lastUpdated", $"gt2025-08-26T20:00+10:00");
        //groupSearchParams.Add("_lastUpdated", $"le2025-08-26T09:00+10:00");
        groupSearchParams.Add("owner:Organization.identifier", hpioSearchToken);
        groupSearchParams.Add("_sort", "_lastUpdated"); 
        groupSearchParams.Add("_count", "500");
        
        IFhirNavigator fhirNavigator = fhirNavigatorFactory.GetFhirNavigator(RepositoryCode);
        
        SearchInfo searchInfo = await fhirNavigator.Search<Hl7.Fhir.Model.Task>(groupSearchParams);
        
        logger.LogInformation("FHIR Server Name  : {FhirServerName}", fhirNavigator.RepositorySettings.DisplayName);
        logger.LogInformation("FHIR Service Base Url : {ServiceBaseUrl}", fhirNavigator.RepositorySettings.ServiceBaseUrl);
        logger.LogInformation("Total pages on the server : {ResourceTotal}", searchInfo.Pages);
        logger.LogInformation("Total resources returned : {ResourceTotal}", searchInfo.ResourceTotal);
        
        var resourceList = fhirNavigator.Cache.GetList<Hl7.Fhir.Model.Task>();

        //CSV Output file
        var csvOutputFileInfo = GetCsvOutputFileInfo();
        await using var writer = new StreamWriter(csvOutputFileInfo.FullName);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<RequestingDoctorInfoMap>();
        
        //Output the .csv header row
        csv.WriteHeader<RequestingDoctorInfo>();
        await csv.NextRecordAsync();
        
        foreach (Hl7.Fhir.Model.Task task in (resourceList))
        {
            PractitionerRole? practitionerRole = await fhirNavigator.GetResource<PractitionerRole>(task.Requester, "task.Requester");
            ArgumentNullException.ThrowIfNull(practitionerRole);

            Identifier? medicareProviderNumberIdentifier = GetMedicareProviderNumberIdentifier(practitionerRole.Identifier);
            ArgumentNullException.ThrowIfNull(medicareProviderNumberIdentifier);
            
            csv.WriteRecord(new RequestingDoctorInfo()
            {
                MedicareProviderNumber = medicareProviderNumberIdentifier.Value, 
                Location = practitionerRole.Location.FirstOrDefault()?.Display, 
                Doctor = practitionerRole.Practitioner.Display, 
                Surgery = practitionerRole.Organization.Display
            });
            
            await csv.NextRecordAsync();
        }

        await csv.FlushAsync();
        fhirNavigator.Cache.Clear();
        logger.LogInformation("Completed");
    }

    private Identifier? GetMedicareProviderNumberIdentifier(List<Identifier> practitionerRoleIdentifier)
    {
        return practitionerRoleIdentifier.FirstOrDefault(x => x.System == "http://ns.electronichealth.net.au/id/medicare-provider-number");
    }

    private FileInfo GetCsvOutputFileInfo()
    {
        DirectoryInfo csvOutputDirectoryInfo = new DirectoryInfo(appConfig.Value.CsvDirectoryPath);
        csvOutputDirectoryInfo.Create();
        FileInfo csvOutputFileInfo = new FileInfo(Path.Combine(csvOutputDirectoryInfo.FullName, $"requesting-doctor-info-{DateTime.Now:yyyyMMddHHmmss}.csv"));
        return csvOutputFileInfo;
    }
}

