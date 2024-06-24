using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stackage.Aws.Lambda.Abstractions;
using Stackage.Aws.Lambda.Extensions;
using Stackage.Aws.Lambda.Middleware;

namespace ToongabbieUtility.RosterDutyReminder;

// TODO: Register any services and/or middleware or remove
public class LambdaStartup : ILambdaStartup
{
   private readonly IConfiguration _configuration;

   public LambdaStartup(IConfiguration configuration)
   {
      _configuration = configuration;
   }

   public void ConfigureServices(IServiceCollection services)
   {
      var dynamoDbConfig = _configuration.GetSection("DynamoDb");
      var runLocalDynamoDb = dynamoDbConfig.GetValue<bool>("LocalMode");

      services.AddDeadlineCancellation(_configuration);
      services.AddAWSService<IAmazonSimpleNotificationService>();
      services.AddDefaultAWSOptions(_configuration.GetAWSOptions());

      #region DynamoDB setup
      if (runLocalDynamoDb)
      {
         services.AddSingleton<IAmazonDynamoDB>(sp =>
         {
            var clientConfig = new AmazonDynamoDBConfig { ServiceURL = dynamoDbConfig.GetValue<string>("LocalServiceUrl") };
            return new AmazonDynamoDBClient(clientConfig);
         });
      }
      else
      {
         services.AddAWSService<IAmazonDynamoDB>();
      }
      services.AddTransient<IDynamoDBContext, DynamoDBContext>(sp =>
      {
         var client = sp.GetService<IAmazonDynamoDB>();
         var dynamoDbContextConfig = new DynamoDBContextConfig
         {
            TableNamePrefix = dynamoDbConfig.GetValue<string>("TableNamePrefix")
         };
         return new DynamoDBContext(client, dynamoDbContextConfig);
      });

      #endregion
   }

   public void ConfigurePipeline(ILambdaPipelineBuilder pipelineBuilder)
   {
      pipelineBuilder.Use<DeadlineCancellationMiddleware>();
   }
}
