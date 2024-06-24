using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToongabbieUtility.Domain;

namespace ToongabbieUtility.RosterDutyReminder;

/// <summary>
/// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
/// </summary>
public class Functions(IDynamoDBContext amazonDynamoDb, IAmazonSimpleNotificationService notificationService, IConfiguration configuration, ILogger<Functions> logger)
{
    /// <summary>
    /// Root route that provides information about the other requests that can be made.
    /// </summary>
    /// <returns>API descriptions.</returns>
    [LambdaFunction]
    public async Task<Response> Handler(Request request, ILambdaContext context)
    {
        // Read all tenants
        var allTenants = await amazonDynamoDb.ScanAsync<ToongabbieTenant>(new List<ScanCondition>())
            .GetRemainingAsync();

        allTenants = allTenants.OrderBy(t => t.RoomNumber).ToList();

        switch (request.Action)
        {
            case ActionType.MoveNextTenant:
                await MoveToNextTenant(allTenants);
                break;
            
            case ActionType.AnnounceDuty:
                await AnnounceRoster(allTenants);
                break;
                
            case ActionType.RemindBinDuty:
                await RemindBinDuty(allTenants);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return new Response { IsSuccessful = true };
    }

    private async Task AnnounceRoster(List<ToongabbieTenant> allTenants)
    {
        var currentOnDutyTenant = allTenants[allTenants.FindIndex(x => x.IsOnRosterDuty)];

        foreach (var tenant in allTenants)
        {
            if (!string.IsNullOrWhiteSpace(tenant.PhoneNumber))
            {
                var snsRequest = new PublishRequest
                {
                    Message =
                        $"Hello everyone. Roster duty from next Monday to Sunday is {currentOnDutyTenant.TenantName}.\n\n" +
                        $"{currentOnDutyTenant.TenantName}, please make sure the bin in the kitchen is cleared " +
                        $"regularly and the wheelie bins are pushed out on Tuesday night.\n\n" +
                        $"Everyone please also clean the common area after you use (kitchen and toilets). Thank you. \n\n" +
                        $"James (Reply to 0430227759)",
                    PhoneNumber = tenant.PhoneNumber,
                };

                try
                {
                    logger.LogInformation($"Sending: {snsRequest.Message} to {tenant.PhoneNumber}");
                    var enabledSms = configuration.GetValue<bool>("EnableSMS");
                    if (enabledSms)
                    {
                        var response = await notificationService.PublishAsync(snsRequest);
                        logger.LogInformation($"Result: {response.HttpStatusCode}, {response.MessageId}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error sending message");
                }
            }
        }
    }

    private async Task MoveToNextTenant(List<ToongabbieTenant> allTenants)
    {
        if (allTenants.Count > 1)
        {
            var currentOnDutyTenantIndex = allTenants.FindIndex(x => x.IsOnRosterDuty);
            var currentOnDutyTenant = allTenants[currentOnDutyTenantIndex];
            ToongabbieTenant? nextOnDutyTenant = null;
            for (int i = 0; i < allTenants.Count; i++)
            {
                var nextOnDutyTenantIndex = (currentOnDutyTenantIndex + i + 1) % allTenants.Count;
                nextOnDutyTenant = allTenants[nextOnDutyTenantIndex];
                if (!string.IsNullOrEmpty(nextOnDutyTenant.PhoneNumber))
                {
                    break;
                }
            }
            logger.LogInformation("Update roster: {@currentOnDutyTenant}, {@nextOnDutyTenant}", currentOnDutyTenant, nextOnDutyTenant);
            currentOnDutyTenant.IsOnRosterDuty = false;
            nextOnDutyTenant!.IsOnRosterDuty = true;
            await amazonDynamoDb.SaveAsync(currentOnDutyTenant);
            await amazonDynamoDb.SaveAsync(nextOnDutyTenant);
        }
    }

    private async Task RemindBinDuty(List<ToongabbieTenant> allTenants)
    {
        ToongabbieTenant currentOnDutyTenant = allTenants.FirstOrDefault(t => t.IsOnRosterDuty);

        if (currentOnDutyTenant == null)
        {
            return;
        }

        var snsRequest = new PublishRequest
        {
            Message =
                $"Hello {currentOnDutyTenant.TenantName}. Please remember to push out the wheelie bins. Also check if yellow bin should be pushed out too.\n\n" +
                $"Thank you. James (Reply to 0430227759)",
            PhoneNumber = currentOnDutyTenant.PhoneNumber,
        };

        try
        {
            logger.LogInformation($"Sending: {snsRequest.Message} to {currentOnDutyTenant.PhoneNumber}");
            var enabledSms = configuration.GetValue<bool>("EnableSMS");
            if (enabledSms)
            {
                var response = await notificationService.PublishAsync(snsRequest);
                logger.LogInformation(
                    $"Result: {response.HttpStatusCode}, {response.MessageId}, {response.ResponseMetadata.ChecksumValidationStatus}");            
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error sending message");
        }
    }
}