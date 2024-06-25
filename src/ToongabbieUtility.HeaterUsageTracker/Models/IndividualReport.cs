namespace ToongabbieUtility.HeaterUsageTracker.Models;

public class IndividualReport
{
    public string Sid { get; set; }
        
    public string SidDescription { get; set; }
        
    public List<string> BreakdownDescription { get; set; } = new List<string>();
        
    public string Summary { get; set; }
}