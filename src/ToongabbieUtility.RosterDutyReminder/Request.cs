namespace ToongabbieUtility.RosterDutyReminder;

public class Request
{
    public ActionType Action { get; set; }
}

public enum ActionType
{
    AnnounceDuty,
    MoveNextTenant,
    RemindBinDuty
}
