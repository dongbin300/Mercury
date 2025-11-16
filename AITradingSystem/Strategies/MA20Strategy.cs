using AITradingSystem.Models;

namespace AITradingSystem.Strategies
{
    public class MA20Strategy : TradingStrategy
    {
        private bool _isInPosition = false;

        public MA20Strategy()
        {
            Name = "MA20 Crossover";
            Parameters["Period"] = 20;
            Parameters["EnableStopLoss"] = false;
            Parameters["StopLossPercent"] = 0.05;
            Parameters["EnableTakeProfit"] = false;
            Parameters["TakeProfitPercent"] = 0.10;
        }

        public override TradeSignal GenerateSignal(List<MarketData> historicalData, MarketData currentData)
        {
            if (historicalData.Count < (int)Parameters["Period"])
                return null;

            var period = (int)Parameters["Period"];
            var ma20 = CalculateMA(historicalData, period);
            var prevMA20 = CalculateMA(historicalData.Take(historicalData.Count - 1).ToList(), period);

            var signal = new TradeSignal
            {
                Timestamp = currentData.Timestamp,
                Price = currentData.Close
            };

            // 매수 조건: 종가가 MA20을 상향 돌파
            if (!_isInPosition && currentData.Close > ma20 && historicalData.Last().Close <= prevMA20)
            {
                signal.Type = "BUY";
                signal.Reason = $"Price crossed above MA{period}";
                signal.Context["MA20"] = ma20;
                signal.Context["PrevMA20"] = prevMA20;
                signal.Context["Volatility"] = CalculateVolatility(historicalData, 20);
                _isInPosition = true;
                return signal;
            }

            // 매도 조건: 종가가 MA20을 하향 돌파
            if (_isInPosition && currentData.Close < ma20 && historicalData.Last().Close >= prevMA20)
            {
                signal.Type = "SELL";
                signal.Reason = $"Price crossed below MA{period}";
                signal.Context["MA20"] = ma20;
                signal.Context["PrevMA20"] = prevMA20;
                signal.Context["Volatility"] = CalculateVolatility(historicalData, 20);
                _isInPosition = false;
                return signal;
            }

            return null;
        }

        public override void UpdateParameters(Dictionary<string, object> newParameters)
        {
            foreach (var param in newParameters)
            {
                Parameters[param.Key] = param.Value;
            }
        }

        private double CalculateMA(List<MarketData> data, int period)
        {
            return data.TakeLast(period).Average(x => x.Close);
        }

        public double CalculateVolatility(List<MarketData> data, int period)
        {
            var returns = data.TakeLast(period + 1)
                .Select((x, i) => i == 0 ? 0 : Math.Log(x.Close / data[data.Count - period - 1 + i - 1].Close))
                .Skip(1);

            var mean = returns.Average();
            return Math.Sqrt(returns.Sum(x => Math.Pow(x - mean, 2)) / period);
        }
    }
}
