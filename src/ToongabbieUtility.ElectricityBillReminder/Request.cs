namespace ToongabbieUtility.ElectricityBillReminder;

public class Request
{
    public Action Action { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public enum Action
{
    SaveWeeklyReport,
    SendSms,
    SendSns,
    All
}