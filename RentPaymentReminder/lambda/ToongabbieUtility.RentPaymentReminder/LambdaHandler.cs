using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Stackage.Aws.Lambda.Abstractions;
using Stackage.Aws.Lambda.Results;
using TimeZoneConverter;
using ToongabbieUtility.Domain;

namespace ToongabbieUtility.RentPaymentReminder
{
   public class LambdaHandler : ILambdaHandler<Request>
   {
      private readonly IDynamoDBContext _amazonDynamoDb;
      private readonly IAmazonSimpleNotificationService _notificationService;
      private readonly TimeProvider _timeProvider;
      private readonly TimeZoneInfo _sydneyTimeZone = TZConvert.GetTimeZoneInfo("Australia/Sydney");

      public LambdaHandler(IDynamoDBContext amazonDynamoDb, IAmazonSimpleNotificationService notificationService, TimeProvider timeProvider)
      {
         _amazonDynamoDb = amazonDynamoDb;
         _notificationService = notificationService;
         _timeProvider = timeProvider;
      }

      public async Task<ILambdaResult> HandleAsync(Request request, ILambdaContext context)
      {
         var now = request.Timestamp ?? _timeProvider.GetUtcNow();
         var sydneyToday = TimeZoneInfo.ConvertTime(now, _sydneyTimeZone).Date;

         var duePayments = await _amazonDynamoDb
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
                  Console.WriteLine($"Sending: {snsRequest.Message} to {duePayment.PhoneNumber}");
                  // var response = await _notificationService.PublishAsync(snsRequest);
                  // Console.Write($"Result: {response.HttpStatusCode}, {response.MessageId}");
               }
               catch (Exception ex)
               {
                  Console.WriteLine($"Error sending message: {ex}");
               }
            }
         }

         return new VoidResult();
      }
   }
}
