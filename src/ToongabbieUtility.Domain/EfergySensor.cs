using Amazon.DynamoDBv2.DataModel;

namespace ToongabbieUtility.Domain;

[DynamoDBTable("EfergySensors")]
public class EfergySensor
{
    [DynamoDBHashKey] public string Sid { get; set; }

    [DynamoDBProperty] public string Description { get; set; }
}