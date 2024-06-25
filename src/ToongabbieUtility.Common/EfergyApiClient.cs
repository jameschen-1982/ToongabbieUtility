using System.Web;
using ToongabbieUtility.Common.Efergy;
using System.Text.Json;

namespace ToongabbieUtility.Common;

public class EfergyApiClient : IEfergyApiClient
{
    private static readonly HttpClient Client = new HttpClient();

    private const string ApiUrl = "http://www.energyhive.com/mobile_proxy/getHV";
        
    public async Task<EfergyDataResponse?> QueryAsync(EfergyRequest request)
    {
        var uri = $"{ApiUrl}?token={HttpUtility.UrlEncode(request.Token)}&" +
                  $"period={HttpUtility.UrlEncode(request.Period)}&" +
                  $"fromTime={request.FromTime}&" +
                  $"toTime={request.ToTime}&" +
                  $"aggperiod={HttpUtility.UrlEncode(request.AggPeriod)}&" +
                  $"aggFunc={HttpUtility.UrlEncode(request.AggFunc)}&" +
                  $"type={HttpUtility.UrlEncode(request.Type)}&" +
                  $"offset={request.Offset}";
            
        var responseString = await Client.GetStringAsync(uri);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        
        if (responseString.Contains("status"))
        {
            return JsonSerializer.Deserialize<EfergyDataResponse>(responseString, options);
        } else if (responseString.Contains("error"))
        {
            var responseError = JsonSerializer.Deserialize<EfergyErrorResponse>(responseString, options);
            throw new EfergyApiException($"Request: {uri}") { ResponseBody = responseError };
        }
        throw new EfergyApiException($"Unexpected error. Request: {uri}");                
    }
}