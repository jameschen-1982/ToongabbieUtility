using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using ToongabbieUtility.ElectricityBillReminder;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>))]

namespace ToongabbieUtility.ElectricityBillReminder;

[JsonSerializable(typeof(Request))]
public partial class LambdaJsonSerializerContext : JsonSerializerContext
{
}
