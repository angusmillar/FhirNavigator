namespace ConsoleApp;

public class ApplicationConfiguration
{
    public const string SectionName = "ApplicationConfiguration";
    
    public required string ApplicationName { get; set; }
    
    public required string CsvDirectoryPath { get; set; }
    
}