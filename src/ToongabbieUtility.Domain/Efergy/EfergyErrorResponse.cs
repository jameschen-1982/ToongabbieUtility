using System.Runtime.Serialization;

namespace ToongabbieUtility.Common.Efergy;

[DataContract]
public class EfergyErrorResponse
{
    [DataMember(Name = "error")]
    public ErrorContent Error { get; set; }
        
    [DataContract]
    public class ErrorContent
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
            
        [DataMember(Name = "desc")]
        public string Desc { get; set; }
            
        [DataMember(Name = "more")]
        public string More { get; set; }
    }
}