using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using TimeZoneConverter;
using ToongabbieUtility.Common;
using ToongabbieUtility.Common.Efergy;
using ToongabbieUtility.Domain;
using ToongabbieUtility.HeaterUsageTracker.Models;

namespace ToongabbieUtility.HeaterUsageTracker;

public class StatisticAggregator(IEfergyApiClient efergyClient, IDynamoDBContext amazonDynamoDb) : IStatisticAggregator
{
    public async Task PullDailyDataAsync(DateTime utcNow, ILambdaContext context)
    {
        // Get time for Sydney Yesterday
        var sydneyYesterday = GetTimeRange(utcNow, out var fromTime, out var toTime);

        // pull the data from Efergy
        var request = new EfergyRequest
        {
            Token = "aXiVyAkDxqe-PFV1ZN4gfxRsLtS7c3wk",
            Period = "custom",
            FromTime = fromTime,
            ToTime = toTime,
            AggPeriod = "minute",
            AggFunc = "avg",
            Type = "PWER%",
            Offset = 0
        };

        var response = await efergyClient.QueryAsync(request);
        // Iterate each sensor
        foreach (var sensorData in response.Data)
        {
            var heaterUsageData = sensorData.Data.Where(d =>
            {
                if (!decimal.TryParse(d.First().Value, out var power))
                {
                    return false;
                }

                return power >= 800;
            }).ToList();
            var totalMinutes = heaterUsageData.Count();
            var totalWattMinutes = heaterUsageData.Sum(d => decimal.Parse(d.First().Value));
            var totalMissingMinutes = sensorData.Data.Count(d => !decimal.TryParse(d.First().Value, out var power));

            await SaveToDynamoDb(sydneyYesterday, sensorData.Sid, totalMinutes, totalWattMinutes, totalMissingMinutes);
        }
    }

    public DateTime GetTimeRange(DateTime utcNow, out long fromTime, out long toTime)
    {
        var utcDateTimeOffset = new DateTimeOffset(utcNow.Ticks, TimeSpan.Zero);
        var tzi = TZConvert.GetTimeZoneInfo("Australia/Sydney");
        var sydneyNow = TimeZoneInfo.ConvertTime(utcDateTimeOffset, tzi);
        var dateTimeOffsetToday = new DateTimeOffset(sydneyNow.Date, tzi.BaseUtcOffset);
        var sydneyYesterday = sydneyNow.Date.AddDays(-1);
        var dateTimeOffsetYesterday = new DateTimeOffset(sydneyYesterday, tzi.BaseUtcOffset);
        fromTime = dateTimeOffsetYesterday.ToUnixTimeSeconds();
        toTime = dateTimeOffsetToday.ToUnixTimeSeconds();

        return sydneyYesterday;
    }

    private async Task SaveToDynamoDb(DateTime date, string sid, int totalMinutes, decimal totalWattMinutes,
        decimal totalMissingMinutes)
    {
        var dailyUsage = new DailyHeaterUsage
        {
            Date = date,
            Sid = sid,
            TotalMinutes = totalMinutes,
            TotalWattMinutes = Convert.ToInt32(totalWattMinutes),
            MissingMinutes = Convert.ToInt32(totalMissingMinutes)
            
        };
        await amazonDynamoDb.SaveAsync(dailyUsage);
    }


    public async Task<Dictionary<string, IndividualReport>> PublishWeeklyDataAsync(DateTime utcNow)
    {
        var utcDateTimeOffset = new DateTimeOffset(utcNow.Ticks, TimeSpan.Zero);
        var tzi = TZConvert.GetTimeZoneInfo("Australia/Sydney");
        var sydneyNow = TimeZoneInfo.ConvertTime(utcDateTimeOffset, tzi);

        var sensors = (await amazonDynamoDb.ScanAsync<EfergySensor>(new List<ScanCondition>()).GetRemainingAsync()).ToList();
        var individualReports = new Dictionary<string, IndividualReport>();
        
        var dates = Enumerable.Range(-7, 7).Select(i => sydneyNow.Date.AddDays(i)).ToList();
        foreach (var sensor in sensors)
        {
            var individualReport = new IndividualReport
            {
                Sid = sensor.Sid,
                SidDescription = sensor.Description
            };
            var totalMinutes = 0;
            var totalKwh = 0m;
            var totalDollarAmount = 0m;
            foreach (var date in dates)
            {
                var item = await amazonDynamoDb.LoadAsync<DailyHeaterUsage>(sensor.Sid, date);
                if (item == null) continue;
                var subtotalMinutes = item.TotalMinutes;
                totalMinutes += subtotalMinutes;
                var subtotalWattMinutes = item.TotalWattMinutes;
                var subtotalKwh = subtotalWattMinutes / 60000m;
                totalKwh += subtotalKwh;
                var subtotalDollarAmount = subtotalKwh * 0.3m;
                totalDollarAmount += subtotalDollarAmount;

                var missingMinutes = item.MissingMinutes;

                individualReport.BreakdownDescription.Add(
                    $"{date:dddd, dd MMMM}: {subtotalMinutes} minutes, {subtotalKwh:0.0} Kwh, {subtotalDollarAmount:C}, missing {missingMinutes} minutes");
            }

            var totalHours = totalMinutes / 60;
            var remainingMinutes = totalMinutes % 60;

            var totalTime = totalHours > 0 ? $"{totalHours} hours {remainingMinutes} mins" : $"{totalMinutes} mins";

            individualReport.Summary = $"Total: {totalTime}, {totalKwh:0.0} Kwh, {totalDollarAmount:C}";
            individualReports.Add(sensor.Sid, individualReport);
        }

        return individualReports;
    }
}