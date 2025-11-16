namespace AITradingSystem.Models
{
    public class BacktestResult
    {
        public List<Trade> Trades { get; set; } = new List<Trade>();
        public double TotalReturn { get; set; }
        public double WinRate { get; set; }
        public double MaxDrawdown { get; set; }
        public double SharpeRatio { get; set; }
        public int TotalTrades { get; set; }
        public double AvgWin { get; set; }
        public double AvgLoss { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();
    }
}