using System.Runtime.Serialization;

namespace ToongabbieUtility.Common.Efergy;

[DataContract]
public class EfergyData
{
    public List<Dictionary<string, string>> Data { get; set; }
        
    public string Sid { get; set; }
        
    public string Units { get; set; }
}