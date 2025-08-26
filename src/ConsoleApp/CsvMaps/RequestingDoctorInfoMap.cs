using ConsoleApp.CsvModels;
using CsvHelper.Configuration;

namespace ConsoleApp.CsvMaps;

public sealed class RequestingDoctorInfoMap: ClassMap<RequestingDoctorInfo>
{
    public RequestingDoctorInfoMap()
    {
        Map(m => m.MedicareProviderNumber).Name("MedicareProviderNumber");
        Map(m => m.Doctor).Name("Doctor");
        Map(m => m.Surgery).Name("Surgery");;
        Map(m => m.Location).Name("Location");
        
        //Map(m => m.DOB).TypeConverterOption.Format("yyyy-MM-dd").TypeConverterOption.NullValues("");
    }
}