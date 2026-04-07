using ConsoleApp.CsvModels;
using CsvHelper.Configuration;

namespace ConsoleApp.CsvMaps;

public sealed class PatientInfoMap: ClassMap<PatientCsvInfo>
{
    public PatientInfoMap()
    {
        Map(m => m.Family).Name("FamilyName");
        Map(m => m.GivenName).Name("FirstName");
        Map(m => m.Gender).Name("Sex");
        
        //Map(m => m.DOB).TypeConverterOption.Format("yyyy-MM-dd").TypeConverterOption.NullValues("");
    }
}