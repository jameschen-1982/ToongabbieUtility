using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Logging;

namespace ToongabbieUtility.HeaterUsageTracker;

public class Function(
    IStatisticAggregator statisticAggregator,
    ILogger<Function> logger,
    IAmazonSimpleNotificationService notificationService,
    TimeProvider timeProvider)
{
    [LambdaFunction]
    public async Task Handler(Request request, ILambdaContext context)
    {
        await statisticAggregator.PullDailyDataAsync(timeProvider.GetUtcNow().UtcDateTime, context);
    }
}