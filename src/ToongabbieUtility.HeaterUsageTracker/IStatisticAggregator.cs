using Amazon.Lambda.Core;
using ToongabbieUtility.HeaterUsageTracker.Models;

namespace ToongabbieUtility.HeaterUsageTracker;

public interface IStatisticAggregator
{
    Task PullDailyDataAsync(DateTime utcNow, ILambdaContext context);

    Task<Dictionary<string, IndividualReport>> PublishWeeklyDataAsync(DateTime utcNow);
}