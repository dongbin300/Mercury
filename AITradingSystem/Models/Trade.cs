namespace AITradingSystem.Models
{
    public class Trade
    {
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public double EntryPrice { get; set; }
        public double? ExitPrice { get; set; }
        public double ProfitLoss { get; set; }
        public double ProfitLossPercent { get; set; }
        public string EntryReason { get; set; }
        public string ExitReason { get; set; }
        public Dictionary<string, object> MarketCondition { get; set; } = new Dictionary<string, object>();
    }
}