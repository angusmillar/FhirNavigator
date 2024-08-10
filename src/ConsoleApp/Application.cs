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
    public async Task Run()
    {
        logger.LogInformation("Running: {ApplicationName}", appConfig.Value.ApplicationName);

        var groupSearchParams = new SearchParams();
        
        groupSearchParams.Add("family", $"Smith");
        groupSearchParams.Add("given", $"John");
        groupSearchParams.Add("_include", $"organization");
        groupSearchParams.Add("_sort", $"_lastUpdated");
        
        IFhirNavigator fhirNavigator = fhirNavigatorFactory.GetFhirNavigator("MyFhirServerName");
        
        SearchInfo searchInfo = await fhirNavigator.Search<Patient>(groupSearchParams, pageLimiter: 100);

        logger.LogInformation("Total pages on the server : {ResourceTotal}", searchInfo.Pages);
        logger.LogInformation("Total resources returned : {ResourceTotal}", searchInfo.ResourceTotal);
        
        var patientResourceList = fhirNavigator.Cache.GetList<Patient>();
        
        Patient? firstPatient = patientResourceList.FirstOrDefault();
        if (firstPatient is not null)
        {
            Organization? managingOrganization =
                await fhirNavigator.GetResource<Organization>(firstPatient.ManagingOrganization, "Patient.ManagingOrganization",
                    firstPatient);
            
            ArgumentNullException.ThrowIfNull(managingOrganization);
            logger.LogInformation("The first Patient's managing organization name was : {Name}", managingOrganization.Name);
        }
        
        fhirNavigator.Cache.Clear();
        

        logger.LogInformation("Completed");
    }
    
}