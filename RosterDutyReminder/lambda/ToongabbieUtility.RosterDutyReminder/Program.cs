using Amazon.Lambda.Serialization.SystemTextJson;
using Stackage.Aws.Lambda;
using ToongabbieUtility.RosterDutyReminder;
using LambdaJsonSerializerContext = ToongabbieUtility.RosterDutyReminder.LambdaJsonSerializerContext;

using var consoleLifetime = new ConsoleLifetime();

await new LambdaListenerBuilder()
   .UseHandler<LambdaHandler, Request>()
   .UseStartup<LambdaStartup>()
   .UseSerializer<SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>>()
   .Build()
   .ListenAsync(consoleLifetime.Token);

