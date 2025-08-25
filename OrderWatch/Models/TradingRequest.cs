namespace OrderWatch.Models;

public class TradingRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal StopPrice { get; set; }
    public string TimeInForce { get; set; } = "GTC";
    public bool ReduceOnly { get; set; }
    public string PositionSide { get; set; } = string.Empty;
    public string WorkingType { get; set; } = "CONTRACT_PRICE";
    public bool ClosePosition { get; set; }
    public decimal? ActivationPrice { get; set; }
    public decimal? CallbackRate { get; set; }
    public string? PriceProtect { get; set; }
    public int? Leverage { get; set; }
}
