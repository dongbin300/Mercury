namespace AITradingSystem.Models
{
    public class TradeSignal
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } // "BUY", "SELL"
        public double Price { get; set; }
        public string Reason { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }
}
