using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Stackage.Aws.Lambda.Abstractions;
using Stackage.Aws.Lambda.Results;
using TimeZoneConverter;
using ToongabbieUtility.Domain;

namespace ToongabbieUtility.ElectricityBillReminder
{
   // TODO: Rename and implement the required behaviour
   public class LambdaHandler : ILambdaHandler<Request>
   {
      private readonly IDynamoDBContext _amazonDynamoDb;
      private readonly IAmazonSimpleNotificationService _notificationService;
      private readonly TimeProvider _timeProvider;

      public LambdaHandler(IDynamoDBContext amazonDynamoDb, IAmazonSimpleNotificationService notificationService, TimeProvider timeProvider)
      {
         _amazonDynamoDb = amazonDynamoDb;
         _notificationService = notificationService;
         _timeProvider = timeProvider;
      }
      public async Task<ILambdaResult> HandleAsync(Request request, ILambdaContext context)
      {
         var allTenants = await _amazonDynamoDb.ScanAsync<ToongabbieTenant>(new[] { new ScanCondition("RoomNumber", ScanOperator.NotEqual, 0) })
            .GetRemainingAsync();

         var now = _timeProvider.GetUtcNow();
         var tzi = TZConvert.GetTimeZoneInfo("Australia/Sydney");
         var sydneyToday = TimeZoneInfo.ConvertTime(now, tzi).Date;

         var heaterUsage = await _amazonDynamoDb
            .QueryAsync<DailyHeaterUsage>("791479", QueryOperator.Between, [sydneyToday.AddDays(-7), sydneyToday]).GetRemainingAsync();

         return new VoidResult();
      }
   }
}
