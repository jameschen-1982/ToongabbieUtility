using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using FakeItEasy;
using Moq;
using NUnit.Framework;
using Stackage.Aws.Lambda.Results;
using ToongabbieUtility.RosterDutyReminder;

namespace RosterDutyReminder.Tests;

public class LambdaHandlerTests
{
   [Test]
   public async Task handler_returns_greetings()
   {
      var context = A.Fake<ILambdaContext>();
      Mock<IDynamoDBContext> mockDynamoDbContext = new();
      Mock<IAmazonSimpleNotificationService> mockSimpleNotificationService = new();
      var handler = new LambdaHandler(mockDynamoDbContext.Object, mockSimpleNotificationService.Object);

      var result = await handler.HandleAsync(new Request { Action = ActionType.MoveNextTenant}, context);

      Assert.That(result, Is.InstanceOf<StringResult>());
      var stringResult = (StringResult)result;
      Assert.That(stringResult.Content, Is.EqualTo("Greetings FOO!"));
   }
}
