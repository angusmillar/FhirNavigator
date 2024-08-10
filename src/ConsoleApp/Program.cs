using ConsoleApp;
using FhirNavigator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("application.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();
    
IServiceCollection services = new ServiceCollection();

services.AddLogging();

//Configuration
services.AddOptions<ApplicationConfiguration>().Bind(configuration.GetSection(ApplicationConfiguration.SectionName));

//Add Services
services.AddScoped<Application>();

FhirNavigatorSettings? fhirNavigatorSettings = configuration.GetRequiredSection(FhirNavigatorSettings.SectionName)
    .Get<FhirNavigatorSettings>();
ArgumentNullException.ThrowIfNull(fhirNavigatorSettings);
services.AddFhirNavigator(settings =>
{
    settings.FhirRepositories = fhirNavigatorSettings.FhirRepositories;
    settings.Proxy = fhirNavigatorSettings.Proxy;
});

var serviceProvider = services.BuildServiceProvider();

serviceProvider.GetRequiredService<ILoggerFactory>()
    .AddSerilog();


var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
await using (var scope = serviceScopeFactory.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<Application>().Run();
}



