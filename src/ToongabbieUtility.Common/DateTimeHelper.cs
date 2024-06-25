namespace ToongabbieUtility.Common;

public static class DateTimeHelper
{
    public static (DateTime mondayBeforeLastSunday, DateTime lastSunday) GetLastWholeWeek(DateTime today)
    {
        var sundayDiff = today.DayOfWeek - DayOfWeek.Sunday;
        if (sundayDiff == 0)
        {
            sundayDiff = 7; // Move one week back.
        }
        var lastSunday = today.AddDays(-sundayDiff);
        var mondayBeforeLastSunday = lastSunday.AddDays(-6);

        return (mondayBeforeLastSunday, lastSunday);
    }
}