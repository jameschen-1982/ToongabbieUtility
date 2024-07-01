using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;
using ToongabbieUtility.Common;
using ToongabbieUtility.Domain;
using ToongabbieUtility.ElectricityBillReminder.Models;

namespace ToongabbieUtility.ElectricityBillReminder;

public class Function(
    IDynamoDBContext amazonDynamoDb,
    IAmazonSimpleNotificationService notificationService,
    IConfiguration configuration,
    ILogger<Function> logger,
    TimeProvider timeProvider)
{
    private readonly TimeZoneInfo _sydneyTimeZone = TZConvert.GetTimeZoneInfo("Australia/Sydney");
    private readonly decimal _electricityUnitPrice = configuration.GetValue<decimal>("ElectricityUnitPrice");

    [LambdaFunction]
    public async Task Handler(Request request, ILambdaContext context)
    {
        var now = request.Timestamp ?? timeProvider.GetUtcNow();
        var sydneyToday = TimeZoneInfo.ConvertTime(now, _sydneyTimeZone).Date;
        var (mondayBeforeLastSunday, lastSunday) = DateTimeHelper.GetLastWholeWeek(sydneyToday);

        var allTenants = await amazonDynamoDb.ScanAsync<ToongabbieTenant>(new List<ScanCondition>())
            .GetRemainingAsync();
        var billedTenants = allTenants.Where(t => !string.IsNullOrEmpty(t.Sid)).ToList();
        var sensors = await amazonDynamoDb.ScanAsync<EfergySensor>(new List<ScanCondition>()).GetRemainingAsync();

        await SaveWeeklyReport(sensors, mondayBeforeLastSunday, lastSunday);
        
        await SendSms(sensors, billedTenants, mondayBeforeLastSunday, lastSunday);
        
        await SendSns();
    }

    private async Task SaveWeeklyReport(List<EfergySensor> sensors, DateTime mondayBeforeLastSunday, DateTime lastSunday)
    {
        var keyValuePairs = await Task.WhenAll(sensors.Select(async s =>
            new KeyValuePair<string, List<DailyHeaterUsage>>(s.Sid,
                await amazonDynamoDb
                    .QueryAsync<DailyHeaterUsage>(s.Sid, QueryOperator.Between,
                        [mondayBeforeLastSunday.ToString("yyyy-MM-dd"), lastSunday.AddHours(24).ToString("yyyy-MM-dd")]).GetRemainingAsync())));

        var heaterUsageBySensor = keyValuePairs
            .ToDictionary(x => x.Key, x =>
                new { Sensor = sensors.First(s => s.Sid == x.Key), Usages = x.Value });

        foreach (var sensor in sensors)
        {
            heaterUsageBySensor.TryGetValue(sensor.Sid, out var sidHeaterUsage);
            TimeSpan? totalTimespan = sidHeaterUsage != null ? ElectricityBillFormatter.GetTotalTimeSpan(sidHeaterUsage.Usages) : null;
            decimal? totalWattHours = sidHeaterUsage != null ? ElectricityBillFormatter.GetTotalKwh(sidHeaterUsage.Usages) : null;
            decimal? amount = sidHeaterUsage != null ? ElectricityBillFormatter.GetTotalAmount(sidHeaterUsage.Usages, _electricityUnitPrice) : null;
            
            var report = new WeeklyHeaterUsage
            {
                Sid = sensor.Sid,
                StartDate = mondayBeforeLastSunday.ToString("yyyy-MM-dd"),
                EndDate = lastSunday.ToString("yyyy-MM-dd"),
                TotalHours = totalTimespan != null ? Convert.ToDecimal(Math.Round(totalTimespan.Value.TotalHours, 1)) : null,
                TotalWattHours = totalWattHours != null ? Math.Round(totalWattHours.Value, 1) : null,
                TotalAmount = amount != null ? Math.Round(amount.Value, 2) : null
            };

            await amazonDynamoDb.SaveAsync(report);
        }
    }

    private async Task SendSms(List<EfergySensor> sensors, List<ToongabbieTenant> billedTenants, DateTime mondayBeforeLastSunday, DateTime lastSunday)
    {
        var billedSensors = sensors.Where(s => billedTenants.Any(t => t.Sid == s.Sid)).ToList();

        var keyValuePairs = await Task.WhenAll(billedSensors.Select(async s =>
            new KeyValuePair<string, List<DailyHeaterUsage>>(s.Sid,
                await amazonDynamoDb
                    .QueryAsync<DailyHeaterUsage>(s.Sid, QueryOperator.Between,
                        [mondayBeforeLastSunday.ToString("yyyy-MM-dd"), lastSunday.AddHours(24).ToString("yyyy-MM-dd")])
                    .GetRemainingAsync())));

        var heaterUsageBySensor = keyValuePairs
            .ToDictionary(x => x.Key, x =>
                new { Sensor = billedSensors.First(s => s.Sid == x.Key), Usages = x.Value });

        foreach (var billedTenant in billedTenants)
        {
            heaterUsageBySensor.TryGetValue(billedTenant.Sid, out var heaterUsageFromTenant);
            if (heaterUsageFromTenant == null ||
                ElectricityBillFormatter.GetTotalAmount(heaterUsageFromTenant.Usages, _electricityUnitPrice) <= 1m
                || string.IsNullOrWhiteSpace(billedTenant.PhoneNumber))
            {
                continue;
            }

            // Start to bill if greater than $1.00
            var message =
                ElectricityBillFormatter.GenerateReportForTenant(heaterUsageFromTenant.Usages, _electricityUnitPrice);
            var finalMessage =
                $"Hi {billedTenant.TenantName}, please find the attached heater usage report for the last week: \r\n\r\n" +
                $"{heaterUsageFromTenant.Sensor.Description}\r\n{message}\r\n\r\n" +
                $"Please add the total to your next payment or pay separately. For questions, please reply to James 0430227759";

            var snsRequest = new PublishRequest
            {
                Message = finalMessage,
                PhoneNumber = billedTenant.PhoneNumber,
            };

            try
            {
                logger.LogInformation("Sending: {Message} to {PhoneNumber}", snsRequest.Message,
                    billedTenant.PhoneNumber);
                var enabledSms = configuration.GetValue<bool>("EnableSMS");
                logger.LogInformation("Enable SMS {Flag}", enabledSms);

                if (enabledSms)
                {
                    var response = await notificationService.PublishAsync(snsRequest);
                    logger.LogInformation("Result: {HttpStatusCode}, {MessageId}", response.HttpStatusCode,
                        response.MessageId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message");
            }
        }
    }

    public async Task<Dictionary<string, IndividualReport>> PublishWeeklyDataAsync(DateTime utcNow)
    {
        var utcDateTimeOffset = new DateTimeOffset(utcNow.Ticks, TimeSpan.Zero);
        var tzi = TZConvert.GetTimeZoneInfo("Australia/Sydney");
        var sydneyNow = TimeZoneInfo.ConvertTime(utcDateTimeOffset, tzi);

        var sensors = (await amazonDynamoDb.ScanAsync<EfergySensor>(new List<ScanCondition>()).GetRemainingAsync())
            .ToList();
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
                var item = await amazonDynamoDb.LoadAsync<DailyHeaterUsage>(sensor.Sid, date.ToString("yyyy-MM-dd"));
                if (item == null)
                {
                    item = await amazonDynamoDb.LoadAsync<DailyHeaterUsage>(sensor.Sid, date.ToString("yyyy-MM-ddT00:00:00.000Z"));
                    if (item == null)
                    {
                        continue;              
                    }
                }
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
    
    
    private async Task SendSns()
    {
        var reports = await PublishWeeklyDataAsync(DateTime.UtcNow);

        logger.LogInformation("{@Reports}", reports);

        var sb = new StringBuilder();
        foreach (var kv in reports)
        {
            sb.AppendLine($"{kv.Value.SidDescription}");
            foreach (var item in kv.Value.BreakdownDescription)
            {
                sb.AppendLine($"{item}");
            }

            sb.AppendLine($"{kv.Value.Summary}");
            sb.AppendLine("");
        }

        var request = new PublishRequest
        {
            TopicArn = configuration.GetValue<string>("HeaterBillTopicArn"),
            Message = sb.ToString()
        };

        logger.LogInformation("SNS ARN: {TopicArn}, Message: {Message}", request.TopicArn, request.Message);

        var response = await notificationService.PublishAsync(request);

        logger.LogInformation("response code: {HttpStatusCode}", response.HttpStatusCode);
    }
}