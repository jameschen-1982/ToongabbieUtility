namespace ToongabbieUtility.ElectricityBillReminder.Models;

public class IndividualReport
{
    public string Sid { get; set; }
        
    public string SidDescription { get; set; }
        
    public List<string> BreakdownDescription { get; set; } = new();
        
    public string Summary { get; set; }
}