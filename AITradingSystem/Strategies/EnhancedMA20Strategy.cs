using AITradingSystem.Models;

using System;
using System.Collections.Generic;
using System.Text;

namespace AITradingSystem.Strategies
{
    public class EnhancedMA20Strategy : MA20Strategy
    {
        private bool _isInPosition = false;

        public EnhancedMA20Strategy()
        {
            Name = "Enhanced MA20 Crossover";
            Parameters["EnableVolatilityFilter"] = false;
            Parameters["MaxVolatility"] = 0.015;
            Parameters["EnableTrendFilter"] = false;
            Parameters["TrendPeriod"] = 50;
            Parameters["MinTrendStrength"] = 0.6;
        }

        public override TradeSignal GenerateSignal(List<MarketData> historicalData, MarketData currentData)
        {
            if (historicalData.Count < Math.Max((int)Parameters["Period"], 50))
                return null;

            // 변동성 필터 체크
            if ((bool)Parameters["EnableVolatilityFilter"])
            {
                var volatility = CalculateVolatility(historicalData, 20);
                if (volatility > (double)Parameters["MaxVolatility"])
                {
                    return null; // 변동성이 높으면 거래하지 않음
                }
            }

            // 추세 필터 체크
            if ((bool)Parameters["EnableTrendFilter"])
            {
                var trendStrength = CalculateTrendStrength(historicalData, (int)Parameters["TrendPeriod"]);
                if (trendStrength < (double)Parameters["MinTrendStrength"])
                {
                    return null; // 추세가 약하면 거래하지 않음
                }
            }

            // 기본 MA20 신호 생성
            return base.GenerateSignal(historicalData, currentData);
        }

        private double CalculateTrendStrength(List<MarketData> data, int period)
        {
            var prices = data.TakeLast(period).Select(x => x.Close).ToList();
            var slope = CalculateSlope(prices);
            var correlation = CalculateCorrelation(prices);

            return Math.Abs(correlation) * Math.Tanh(Math.Abs(slope) * 1000);
        }

        private double CalculateSlope(List<double> prices)
        {
            int n = prices.Count;
            double sumX = n * (n - 1) / 2.0;
            double sumY = prices.Sum();
            double sumXY = prices.Select((price, i) => i * price).Sum();
            double sumX2 = n * (n - 1) * (2 * n - 1) / 6.0;

            return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }

        private double CalculateCorrelation(List<double> prices)
        {
            var indices = Enumerable.Range(0, prices.Count).Select(i => (double)i).ToList();

            var meanX = indices.Average();
            var meanY = prices.Average();

            var numerator = indices.Zip(prices, (x, y) => (x - meanX) * (y - meanY)).Sum();
            var denomX = Math.Sqrt(indices.Sum(x => Math.Pow(x - meanX, 2)));
            var denomY = Math.Sqrt(prices.Sum(y => Math.Pow(y - meanY, 2)));

            return numerator / (denomX * denomY);
        }
    }
}
