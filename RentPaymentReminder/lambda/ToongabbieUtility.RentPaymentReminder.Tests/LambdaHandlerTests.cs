using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using FakeItEasy;
using Moq;
using NUnit.Framework;
using Stackage.Aws.Lambda.Results;

namespace ToongabbieUtility.RentPaymentReminder.Tests;

public class LambdaHandlerTests
{
   [Test]
   public async Task handler_returns_greetings()
   {
      Mock<IDynamoDBContext> mockDynamoDbContext = new();
      Mock<IAmazonSimpleNotificationService> mockSimpleNotificationService = new();

      var context = A.Fake<ILambdaContext>();
      var handler = new LambdaHandler(mockDynamoDbContext.Object, mockSimpleNotificationService.Object, TimeProvider.System);

      var result = await handler.HandleAsync(new Request {Timestamp = new DateTimeOffset(2024, 05, 02, 0, 0, 0, TimeSpan.Zero)}, context);

      Assert.That(result, Is.InstanceOf<StringResult>());
      var stringResult = (StringResult)result;
      Assert.That(stringResult.Content, Is.EqualTo("Greetings FOO!"));
   }
}
