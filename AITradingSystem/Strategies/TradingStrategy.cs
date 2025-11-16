using AITradingSystem.Models;

namespace AITradingSystem.Strategies
{
    public abstract class TradingStrategy
    {
        public string Name { get; protected set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public abstract TradeSignal GenerateSignal(List<MarketData> historicalData, MarketData currentData);
        public abstract void UpdateParameters(Dictionary<string, object> newParameters);
    }
}