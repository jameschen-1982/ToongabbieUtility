using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using ToongabbieUtility.HeaterUsageTracker;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>))]

namespace ToongabbieUtility.HeaterUsageTracker;

[JsonSerializable(typeof(Request))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
public partial class LambdaJsonSerializerContext : JsonSerializerContext
{
    
}