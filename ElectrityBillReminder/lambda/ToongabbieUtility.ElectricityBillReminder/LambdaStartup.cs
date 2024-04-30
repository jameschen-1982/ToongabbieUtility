using System;
using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stackage.Aws.Lambda.Abstractions;
using Stackage.Aws.Lambda.Extensions;
using Stackage.Aws.Lambda.Middleware;

namespace ToongabbieUtility.ElectricityBillReminder;

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
      services.AddDeadlineCancellation(_configuration);
      services.AddAWSService<IAmazonDynamoDB>();
      services.AddSingleton(TimeProvider.System);
      services.AddAWSService<IAmazonSimpleNotificationService>();
      services.AddDefaultAWSOptions(_configuration.GetAWSOptions());
      services.AddTransient<IDynamoDBContext, DynamoDBContext>(sp =>
      {
         var client = sp.GetService<IAmazonDynamoDB>();
         return new(client);
      });
   }

   public void ConfigurePipeline(ILambdaPipelineBuilder pipelineBuilder)
   {
      pipelineBuilder.Use<DeadlineCancellationMiddleware>();
   }
}
