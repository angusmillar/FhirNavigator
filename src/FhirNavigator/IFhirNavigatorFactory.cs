namespace FhirNavigator;

public interface IFhirNavigatorFactory
{
    IFhirNavigator GetFhirNavigator(string repositoryCode);
}