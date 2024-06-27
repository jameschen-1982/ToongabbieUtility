using Amazon.DynamoDBv2.DataModel;

namespace ToongabbieUtility.Domain;

[DynamoDBTable("DailyHeaterUsage")]
public class DailyHeaterUsage
{
    [DynamoDBHashKey] public string Sid { get; set; }

    [DynamoDBRangeKey] public string Date { get; set; }
    
    [DynamoDBProperty] public int TotalMinutes { get; set; }

    [DynamoDBProperty] public int TotalWattMinutes { get; set; }

    [DynamoDBProperty] public int MissingMinutes { get; set; }
}