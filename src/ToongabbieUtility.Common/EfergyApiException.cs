using ToongabbieUtility.Common.Efergy;

namespace ToongabbieUtility.Common;

public class EfergyApiException : Exception
{
    public EfergyApiException(string message) : base(message)
    {
            
    }
        
    public EfergyErrorResponse ResponseBody { get; set; }
}