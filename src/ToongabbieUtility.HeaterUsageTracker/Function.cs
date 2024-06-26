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
    IAmazonSimpleNotificationService notificationService)
{
    [LambdaFunction]
    public async Task Handler(Request request, ILambdaContext context)
    {
        for (int i = 7; i >= 0; i--)
        {
            await statisticAggregator.PullDailyDataAsync(DateTime.UtcNow.AddDays(-i), context);
        }
    }
}