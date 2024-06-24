using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using ToongabbieUtility.RosterDutyReminder;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>))]

namespace ToongabbieUtility.RosterDutyReminder;

[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Response))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
public partial class LambdaJsonSerializerContext : JsonSerializerContext
{
}
