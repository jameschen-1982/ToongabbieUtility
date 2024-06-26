using Amazon.Lambda.Core;

namespace ToongabbieUtility.HeaterUsageTracker;

public interface IStatisticAggregator
{
    Task PullDailyDataAsync(DateTime utcNow, ILambdaContext context);
}