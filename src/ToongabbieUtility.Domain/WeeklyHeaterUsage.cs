using Amazon.DynamoDBv2.DataModel;

namespace ToongabbieUtility.Domain;

[DynamoDBTable("WeeklyHeaterUsage")]
public class WeeklyHeaterUsage
{
    [DynamoDBHashKey] public string Sid { get; set; }

    [DynamoDBRangeKey] public string StartDate { get; set; }
    
    [DynamoDBProperty] public string EndDate { get; set; }
    
    [DynamoDBProperty] public decimal? TotalHours { get; set; }

    [DynamoDBProperty] public decimal? TotalWattHours { get; set; }

    [DynamoDBProperty] public decimal? TotalAmount { get; set; }
}