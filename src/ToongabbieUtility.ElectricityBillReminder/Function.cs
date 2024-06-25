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

         var sensors = (await amazonDynamoDb.ScanAsync<EfergySensor>(new List<ScanCondition>()).GetRemainingAsync())
            .Where(s => billedTenants.Any(t => t.Sid == s.Sid)).ToList();

         var keyValuePairs = await Task.WhenAll(sensors.Select(async s =>
            new KeyValuePair<string, List<DailyHeaterUsage>>(s.Sid,
               await amazonDynamoDb.QueryAsync<DailyHeaterUsage>(s.Sid, QueryOperator.Between, [mondayBeforeLastSunday, lastSunday.AddHours(24)]).GetRemainingAsync())));

         var heaterUsageBySensor = keyValuePairs
            .ToDictionary(x => x.Key, x =>
               new { Sensor = sensors.First(s => s.Sid == x.Key), Usages = x.Value });

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
            var message = ElectricityBillFormatter.GenerateReportForTenant(heaterUsageFromTenant.Usages, _electricityUnitPrice);
            var finalMessage = $"Hi {billedTenant.TenantName}, please find the attached heater usage report for the last week: \r\n\r\n" +
                               $"{heaterUsageFromTenant.Sensor.Description}\r\n{message}\r\n\r\n" +
                               $"Please add the total to your next payment or pay separately. For questions, please reply to James 0430227759";

            var snsRequest = new PublishRequest
            {
               Message = finalMessage,
               PhoneNumber = billedTenant.PhoneNumber,
            };

            try
            {
               logger.LogInformation("Sending: {Message} to {PhoneNumber}",snsRequest.Message, billedTenant.PhoneNumber);
               var enabledSms = configuration.GetValue<bool>("EnableSMS");
               if (enabledSms)
               {
                  var response = await notificationService.PublishAsync(snsRequest);
                  logger.LogInformation("Result: {HttpStatusCode}, {MessageId}", response.HttpStatusCode, response.MessageId);
               }
            }
            catch (Exception ex)
            {
               logger.LogError(ex, "Error sending message");
            }
         }
    }
}