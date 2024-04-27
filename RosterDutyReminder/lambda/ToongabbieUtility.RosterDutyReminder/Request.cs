using System.Text.Json.Serialization;

namespace ToongabbieUtility.RosterDutyReminder;

// TODO: Rename and implement the required arguments or remove and use a raw Stream instead
public class Request
{
   public ActionType Action { get; set; }
}

public enum ActionType
{
   MoveNextTenant,
   RemindBinDuty
}
