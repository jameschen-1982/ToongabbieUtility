using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Stackage.Aws.Lambda.Abstractions;
using Stackage.Aws.Lambda.Results;
using ToongabbieUtility.Domain;

namespace ToongabbieUtility.RosterDutyReminder
{
   // TODO: Rename and implement the required behaviour
   public class LambdaHandler(IDynamoDBContext amazonDynamoDb, IAmazonSimpleNotificationService notificationService)
      : ILambdaHandler<Request>
   {
      public async Task<ILambdaResult> HandleAsync(Request request, ILambdaContext context)
      {

         // Read all tenants
         var allTenants = await amazonDynamoDb.ScanAsync<ToongabbieTenant>(new List<ScanCondition>())
            .GetRemainingAsync();

         allTenants = allTenants.OrderBy(t => t.RoomNumber).ToList();

         if (request.Action == ActionType.MoveNextTenant)
         {
            await MoveToNextTenant(allTenants);
         }
         else if (request.Action == ActionType.RemindBinDuty)
         {
            await RemindBinDuty(allTenants);
         }
         return new VoidResult();
      }

      private async Task MoveToNextTenant(List<ToongabbieTenant> allTenants)
      {
         ToongabbieTenant currentOnDutyTenant, nextOnDutyTenant;

         if (allTenants.Count > 1)
         {
            if (allTenants.FindIndex(x => x.IsOnRosterDuty) == allTenants.Count - 1)
            {
               // Last room
               currentOnDutyTenant = allTenants.Last();
               nextOnDutyTenant = allTenants.First();
            }
            else
            {
               currentOnDutyTenant = allTenants[allTenants.FindIndex(x => x.IsOnRosterDuty)];
               nextOnDutyTenant = allTenants[allTenants.FindIndex(x => x.IsOnRosterDuty) + 1];
            }
            currentOnDutyTenant.IsOnRosterDuty = false;
            nextOnDutyTenant.IsOnRosterDuty = true;
            await amazonDynamoDb.SaveAsync(currentOnDutyTenant);
            await amazonDynamoDb.SaveAsync(nextOnDutyTenant);
         }
         else
         {
            // Do nothing
            return;
         }

         foreach (var tenant in allTenants)
         {
            if (!string.IsNullOrWhiteSpace(tenant.PhoneNumber))
            {
               var snsRequest = new PublishRequest
               {
                  Message = $"Hello everyone. Roster duty from next Monday to Sunday is {currentOnDutyTenant.TenantName}.\n\n" +
                            $"{currentOnDutyTenant.TenantName}, please make sure the bin in the kitchen is cleared " +
                            $"regularly and the wheelie bins are pushed out on Tuesday night.\n\n" +
                            $"Everyone please also clean the common area after you use (kitchen and toilets). Thank you. \n\n" +
                            $"James (Reply to 0430227759)",
                  PhoneNumber = tenant.PhoneNumber,
               };

               try
               {
                  Console.WriteLine($"Sending: {snsRequest.Message} to {tenant.PhoneNumber}");
                  var response = await notificationService.PublishAsync(snsRequest);
                  Console.Write($"Result: {response.HttpStatusCode}, {response.MessageId}");
               }
               catch (Exception ex)
               {
                  Console.WriteLine($"Error sending message: {ex}");
               }
            }
         }
      }

      private async Task RemindBinDuty(List<ToongabbieTenant> allTenants)
      {
         ToongabbieTenant currentOnDutyTenant = allTenants.FirstOrDefault(t => t.IsOnRosterDuty);

         if (currentOnDutyTenant == null)
         {
            Console.WriteLine("No one is on duty");
            return;
         }

         var snsRequest = new PublishRequest
         {
            Message = $"Hello {currentOnDutyTenant.TenantName}. Please remember to push out the wheelie bins. Also check if yellow bin should be pushed out too.\n\n" +
                      $"Thank you. James (Reply to 0430227759)",
            PhoneNumber = currentOnDutyTenant.PhoneNumber,
         };

         try
         {
            Console.WriteLine($"Sending: {snsRequest.Message} to {currentOnDutyTenant.PhoneNumber}");
            var response = await notificationService.PublishAsync(snsRequest);
            Console.WriteLine($"Result: {response.HttpStatusCode}, {response.MessageId}, {response.ResponseMetadata.ChecksumValidationStatus}");
         }
         catch (Exception ex)
         {
            Console.WriteLine($"Error sending message: {ex}");
         }
      }
   }
}
