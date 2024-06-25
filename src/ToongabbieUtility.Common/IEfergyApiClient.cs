using ToongabbieUtility.Common.Efergy;

namespace ToongabbieUtility.Common;

public interface IEfergyApiClient
{
    Task<EfergyDataResponse?> QueryAsync(EfergyRequest request);
}