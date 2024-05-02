using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stackage.Aws.Lambda.Abstractions;
using Stackage.Aws.Lambda.Extensions;
using Stackage.Aws.Lambda.Middleware;

namespace ToongabbieUtility.RentPaymentReminder;

public class LambdaStartup : ILambdaStartup
{
   private readonly IConfiguration _configuration;

   public LambdaStartup(IConfiguration configuration)
   {
      _configuration = configuration;
   }

   public void ConfigureServices(IServiceCollection services)
   {
      services.AddDeadlineCancellation(_configuration);
      services.AddSingleton(TimeProvider.System);

      services.AddDefaultAWSOptions(_configuration.GetAWSOptions());
      services.AddAWSService<IAmazonDynamoDB>();
      services.AddAWSService<IAmazonSimpleNotificationService>();
      services.AddTransient<IDynamoDBContext, DynamoDBContext>(sp =>
      {
         var client = sp.GetService<IAmazonDynamoDB>();
         return new DynamoDBContext(client);
      });
   }

   public void ConfigurePipeline(ILambdaPipelineBuilder pipelineBuilder)
   {
      pipelineBuilder.Use<DeadlineCancellationMiddleware>();
   }
}
