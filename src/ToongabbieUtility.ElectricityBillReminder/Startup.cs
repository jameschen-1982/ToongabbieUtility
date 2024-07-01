using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ToongabbieUtility.ElectricityBillReminder;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        
        #region Configuration setup
        var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", false)
                            .AddEnvironmentVariables();
        var configuration = builder.Build();
        
        if (!string.IsNullOrEmpty(configuration.GetValue<string>("AppConfig:ApplicationId")))
        {
            builder.AddAppConfig(applicationId: configuration.GetValue<string>("AppConfig:ApplicationId"),
                environmentId: configuration.GetValue<string>("AppConfig:EnvironmentId"),
                configProfileId: configuration.GetValue<string>("AppConfig:ConfigProfileId"),
                optional: false,
                reloadAfter: TimeSpan.FromSeconds(configuration.GetValue<int>("AppConfig:ReloadInSeconds")));
            configuration = builder.Build();
        }

        services.AddSingleton<IConfiguration>(configuration);
        #endregion
        
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger(), dispose: true));

        
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
