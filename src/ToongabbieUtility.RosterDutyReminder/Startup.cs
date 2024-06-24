using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace ToongabbieUtility.RosterDutyReminder;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    /// <summary>
    /// Services for Lambda functions can be registered in the services dependency injection container in this method. 
    ///
    /// The services can be injected into the Lambda function through the containing type's constructor or as a
    /// parameter in the Lambda function using the FromService attribute. Services injected for the constructor have
    /// the lifetime of the Lambda compute container. Services injected as parameters are created within the scope
    /// of the function invocation.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", true);

        //// Add AWS Systems Manager as a potential provider for the configuration. This is 
        //// available with the Amazon.Extensions.Configuration.SystemsManager NuGet package.
        //builder.AddSystemsManager("/app/settings");

        var configuration = builder.Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger(), dispose: true));
        
        //// Example of using the AWSSDK.Extensions.NETCore.Setup NuGet package to add
        //// the Amazon S3 service client to the dependency injection container.
        //services.AddAWSService<Amazon.S3.IAmazonS3>();
        
        services.AddAWSService<IAmazonSimpleNotificationService>();
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());

        #region DynamoDB setup
        var dynamoDbConfig = configuration.GetSection("DynamoDb");
        var runLocalDynamoDb = dynamoDbConfig.GetValue<bool>("LocalMode");
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
}
