using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using TimeZoneConverter;
using ToongabbieUtility.Common;
using ToongabbieUtility.Common.Efergy;
using ToongabbieUtility.Domain;

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
    
}