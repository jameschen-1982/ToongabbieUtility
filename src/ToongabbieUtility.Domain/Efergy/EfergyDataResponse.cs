using System.Runtime.Serialization;

namespace ToongabbieUtility.Common.Efergy;

[DataContract]
public class EfergyDataResponse
{
    public string Status { get; set; }
        
    public List<EfergyData> Data { get; set; }
}