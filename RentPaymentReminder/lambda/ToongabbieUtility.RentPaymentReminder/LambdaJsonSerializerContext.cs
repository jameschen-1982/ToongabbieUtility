using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using ToongabbieUtility.RentPaymentReminder;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>))]

namespace ToongabbieUtility.RentPaymentReminder;

[JsonSerializable(typeof(Request))]
public partial class LambdaJsonSerializerContext : JsonSerializerContext
{
}
