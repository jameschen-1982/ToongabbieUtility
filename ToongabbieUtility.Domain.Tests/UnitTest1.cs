using System.Text.Json;
using ToongabbieUtility.Common.Efergy;

namespace ToongabbieUtility.Domain.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        using var fs = File.OpenRead("testData.json");
        using var sr = new StreamReader(fs);
        var jsonString = sr.ReadToEnd();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var resultData = JsonSerializer.Deserialize<EfergyDataResponse>(jsonString, options);
    }
}