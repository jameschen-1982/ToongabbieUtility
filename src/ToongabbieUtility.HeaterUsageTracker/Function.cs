using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using ToongabbieUtility.HeaterUsageTracker.Models;

namespace ToongabbieUtility.HeaterUsageTracker;

public class Function(
    IStatisticAggregator statisticAggregator,
    ILogger<Function> logger,
    IAmazonSimpleNotificationService notificationService)
{
    [LambdaFunction]
    public async Task Handler(Request request, ILambdaContext context)
    {
        try
        {
            for (int i = 7; i >= 0; i--)
            {
                await statisticAggregator.PullDailyDataAsync(DateTime.UtcNow.AddDays(-i), context);
            }

            var reports = await statisticAggregator.PublishWeeklyDataAsync(DateTime.UtcNow);
            
            var json = JsonSerializer.Serialize(reports);

            context.Logger.LogLine(json);

            await SendSNS(reports, context);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error");
            throw;
        }
    }

    private async Task SendSNS(Dictionary<string, IndividualReport> reports, ILambdaContext context)
    {
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
            TopicArn = System.Environment.GetEnvironmentVariable("SNS_TOPIC"),
            Message = sb.ToString()
        };

        logger.LogInformation("SNS ARN: {TopicArn}, Message: {Message}", request.TopicArn, request.Message);

        // var response = await notificationService.PublishAsync(request);

        // context.Logger.LogLine($"response code: {response.HttpStatusCode}");
    }
}