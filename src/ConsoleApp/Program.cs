using ConsoleApp;
using FhirNavigator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("application.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddSerilog();

// IServiceCollection
IServiceCollection services = builder.Services;

// Configuration
IConfiguration configuration = builder.Configuration;
services.AddOptions<ApplicationConfiguration>()
    .Bind(configuration.GetSection(ApplicationConfiguration.SectionName));

// Add Services
services.AddScoped<Application>();

//Set up the FhirNavigator
FhirNavigatorSettings? fhirNavigatorSettings = configuration.GetRequiredSection(FhirNavigatorSettings.SectionName)
    .Get<FhirNavigatorSettings>();
ArgumentNullException.ThrowIfNull(fhirNavigatorSettings);

services.AddFhirNavigator(settings =>
{
    settings.FhirRepositories = fhirNavigatorSettings.FhirRepositories;
    settings.Proxy = fhirNavigatorSettings.Proxy;
});

//Build the host and resolve Application via a scope
using var host = builder.Build();

//Create a new scope
await using var scope = host.Services.CreateAsyncScope();

//Get the Application and run it
await scope.ServiceProvider.GetRequiredService<Application>().Run();