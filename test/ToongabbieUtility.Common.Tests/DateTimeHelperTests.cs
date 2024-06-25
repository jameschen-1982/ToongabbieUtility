using FluentAssertions;

namespace ToongabbieUtility.Common.Tests;

public class DateTimeHelperTests
{
    [Test]
    public void DateTimeHelper_Test1()
    {
        var today = new DateTime(2024, 4, 29);
        var (mondayBeforeLastSunday, lastSunday) = DateTimeHelper.GetLastWholeWeek(today);
        mondayBeforeLastSunday.Should().Be(new DateTime(2024, 4, 22));
        lastSunday.Should().Be(new DateTime(2024, 4, 28));
    }
    
    [Test]
    public void DateTimeHelper_Test2()
    {
        var today = new DateTime(2024, 5, 5);
        var (mondayBeforeLastSunday, lastSunday) = DateTimeHelper.GetLastWholeWeek(today);
        mondayBeforeLastSunday.Should().Be(new DateTime(2024, 4, 22));
        lastSunday.Should().Be(new DateTime(2024, 4, 28));
    }
}