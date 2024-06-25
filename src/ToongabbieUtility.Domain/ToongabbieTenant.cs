using Amazon.DynamoDBv2.DataModel;

namespace ToongabbieUtility.Domain;

[DynamoDBTable("ToongabbieTenants")]
public class ToongabbieTenant
{
    [DynamoDBHashKey] //Partition key
    public int RoomNumber { get; set; }

    [DynamoDBProperty] public string TenantName { get; set; }
    [DynamoDBProperty] public string PhoneNumber { get; set; }
    [DynamoDBProperty] public string Sid { get; set; }
    [DynamoDBProperty] public bool IsOnRosterDuty { get; set; }
    [DynamoDBProperty] public string PaymentDue { get; set; }
}