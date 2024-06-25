namespace ToongabbieUtility.Common.Efergy;

public class EfergyRequest
{
    public string Token { get; set; }
        
    public string Period { get; set; }
        
    public long FromTime { get; set; }
        
    public long ToTime { get; set; }
        
    public string AggPeriod { get; set; }
        
    public string AggFunc { get; set; }
        
    public string Type { get; set; }
        
    public int Offset { get; set; }
}