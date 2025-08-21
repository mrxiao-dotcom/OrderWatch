namespace OrderWatch.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ExceptionDetails { get; set; }
    public string? Symbol { get; set; }
    public string? Action { get; set; }
    public decimal? Price { get; set; }
    public decimal? Quantity { get; set; }
    public string? Result { get; set; }
    
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string FormattedMessage => $"[{Level}] {Message}";
}
