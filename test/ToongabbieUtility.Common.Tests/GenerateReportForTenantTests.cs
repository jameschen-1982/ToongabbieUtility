using FluentAssertions;
using ToongabbieUtility.Domain;

namespace ToongabbieUtility.Common.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void GenerateReportForTenant_Test()
    {
        var unitPrice = 0.3m;
        var testData = new List<DailyHeaterUsage>
        {
            new()
            {
                Date = "2024-04-29",
                TotalMinutes = 30,
                TotalWattMinutes = 10000
            },
            new()
            {
                Date = "2024-04-30",
                TotalMinutes = 61,
                TotalWattMinutes = 60000
            }
        };
         var result = ElectricityBillFormatter.GenerateReportForTenant(testData, unitPrice);

         result.Should().Be(
@"Monday, 29 April: 0h 30m, 0.2Kwh, $0.05
Tuesday, 30 April: 1h 01m, 1.0Kwh, $0.30
Total: 1h 31m, 1.2Kwh, $0.35");
    }
}