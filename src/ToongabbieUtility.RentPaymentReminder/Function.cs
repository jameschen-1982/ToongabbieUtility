using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;
using ToongabbieUtility.Domain;


namespace ToongabbieUtility.RentPaymentReminder;

public class Function(
    IDynamoDBContext amazonDynamoDb,
    IAmazonSimpleNotificationService notificationService,
    IConfiguration configuration,
    ILogger<Function> logger,
    TimeProvider timeProvider)
{
    private readonly TimeZoneInfo _sydneyTimeZone = TZConvert.GetTimeZoneInfo("Australia/Sydney");
    
    [LambdaFunction]
    public async Task Handler(Request request, ILambdaContext context)
    {
        var now = request.Timestamp ?? timeProvider.GetUtcNow();
        var sydneyToday = TimeZoneInfo.ConvertTime(now, _sydneyTimeZone).Date;

        var duePayments = await amazonDynamoDb
            .ScanAsync<ToongabbieTenant>(
                new [] { new ScanCondition("PaymentDue", ScanOperator.Equal, sydneyToday.ToString("yyyy-MM-dd")) })
            .GetRemainingAsync();


        foreach (var duePayment in duePayments)
        {
            var message =
                $"Hi {duePayment.TenantName}, it's a reminder that your rent is due today. If you haven't paid the rent for this week, " +
                $"please make the transfer as soon as possible. If you have done that, please kindly disregard this message and have a nice day. " +
                $"Any question please reply to James 0430227759";

            if (!string.IsNullOrWhiteSpace(duePayment.PhoneNumber))
            {
                var snsRequest = new PublishRequest
                {
                    Message = message,
                    PhoneNumber = duePayment.PhoneNumber,
                };

                try
                {
                    logger.LogInformation("Sending: {Message} to {PhoneNumber}", snsRequest.Message, duePayment.PhoneNumber);
                    var enabledSms = configuration.GetValue<bool>("EnableSMS");
                    if (enabledSms)
                    {
                        var response = await notificationService.PublishAsync(snsRequest);
                        logger.LogInformation("Result: {HttpStatusCode}, {MessageId}", response.HttpStatusCode, response.MessageId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error sending message");
                }
            }
        }
    }
}