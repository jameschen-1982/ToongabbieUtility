using Amazon.DynamoDBv2.DataModel;

namespace ToongabbieUtility.Domain;

[DynamoDBTable("test5-app-DDBTable-1M2H022KQT2KL")]
public class DailyHeaterUsage
{
    [DynamoDBHashKey] public string Sid { get; set; }

    [DynamoDBRangeKey] public DateTime Date { get; set; }
    
    [DynamoDBProperty] public int TotalMinutes { get; set; }

    [DynamoDBProperty] public int TotalWattMinutes { get; set; }

    [DynamoDBProperty] public int MissingMinutes { get; set; }
}