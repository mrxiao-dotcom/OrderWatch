using OrderWatch.Models;

namespace OrderWatch.Services;

public interface IBinanceService
{
    // 账户相关
    Task<AccountInfo?> GetAccountInfoAsync();
    Task<List<PositionInfo>> GetPositionsAsync();
    Task<List<OrderInfo>> GetOpenOrdersAsync();
    Task<List<OrderInfo>> GetAllOrdersAsync(string symbol, long? orderId = null, int? limit = null);
    
    // 交易相关
    Task<bool> PlaceOrderAsync(TradingRequest request);
    Task<bool> CancelOrderAsync(string symbol, long orderId);
    Task<bool> CancelAllOrdersAsync(string symbol);
    Task<decimal> GetLatestPriceAsync(string symbol);
    Task<decimal> Get24hrPriceChangeAsync(string symbol);
    
    // 配置相关
    Task<bool> SetLeverageAsync(string symbol, int leverage);
    Task<bool> SetMarginTypeAsync(string symbol, string marginType);
    
    // 连接管理
    Task<bool> TestConnectionAsync();
    void SetCredentials(string apiKey, string secretKey, bool isTestNet);
}
