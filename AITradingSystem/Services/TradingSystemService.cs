// TradingSystemService.cs에 추가해야 할 메서드들

using AITradingSystem.Models;
using AITradingSystem.Strategies;

namespace AITradingSystem.Services
{
    public class TradingSystemService
    {
        private TradingStrategy _currentStrategy;
        private readonly StrategyAnalyzer _analyzer;
        private List<MarketData> _marketData;

        public TradingSystemService()
        {
            _analyzer = new StrategyAnalyzer();
            _currentStrategy = new MA20Strategy();
            _marketData = new List<MarketData>();
        }

        public async Task<BacktestResult> RunBacktestAsync(List<MarketData> data)
        {
            var result = new BacktestResult();
            var trades = new List<Trade>();
            Trade currentTrade = null;

            // 이전 데이터 클리어
            _marketData.Clear();

            foreach (var candle in data)
            {
                var signal = _currentStrategy.GenerateSignal(_marketData, candle);
                _marketData.Add(candle);

                if (signal != null)
                {
                    if (signal.Type == "BUY" && currentTrade == null)
                    {
                        currentTrade = new Trade
                        {
                            EntryTime = signal.Timestamp,
                            EntryPrice = signal.Price,
                            EntryReason = signal.Reason,
                            MarketCondition = new Dictionary<string, object>(signal.Context)
                        };
                    }
                    else if (signal.Type == "SELL" && currentTrade != null)
                    {
                        currentTrade.ExitTime = signal.Timestamp;
                        currentTrade.ExitPrice = signal.Price;
                        currentTrade.ExitReason = signal.Reason;
                        currentTrade.ProfitLoss = signal.Price - currentTrade.EntryPrice;
                        currentTrade.ProfitLossPercent = (signal.Price - currentTrade.EntryPrice) / currentTrade.EntryPrice * 100;

                        trades.Add(currentTrade);
                        currentTrade = null;
                    }
                }
            }

            result.Trades = trades;
            CalculateMetrics(result);

            return await Task.FromResult(result);
        }

        public async Task<AnalysisResult> AnalyzeAndImproveAsync(BacktestResult backtestResult)
        {
            var analysis = _analyzer.AnalyzeBacktestResult(backtestResult, _marketData);

            // 가장 유망한 개선 제안 적용
            var bestSuggestion = analysis.ImprovementSuggestions
                .OrderByDescending(s => s.ExpectedImprovement)
                .FirstOrDefault();

            if (bestSuggestion != null)
            {
                // 전략을 개선된 버전으로 업그레이드
                _currentStrategy = CreateImprovedStrategy();
                _currentStrategy.UpdateParameters(bestSuggestion.NewParameters);

                System.Console.WriteLine($"전략 개선 적용: {bestSuggestion.Description}");
                System.Console.WriteLine($"예상 개선 효과: {bestSuggestion.ExpectedImprovement:F2}");
            }

            return await Task.FromResult(analysis);
        }

        public string GetCurrentStrategyInfo()
        {
            var info = $"전략: {_currentStrategy.Name}\n";
            info += "파라미터:\n";
            foreach (var param in _currentStrategy.Parameters)
            {
                info += $"  {param.Key}: {param.Value}\n";
            }
            return info;
        }

        public TradingStrategy GetCurrentStrategy()
        {
            return _currentStrategy;
        }

        public async Task UpdateStrategy(Dictionary<string, object> newParameters)
        {
            _currentStrategy.UpdateParameters(newParameters);
            await Task.CompletedTask;
        }

        public void ResetStrategy()
        {
            _currentStrategy = new MA20Strategy();
            _marketData.Clear();
        }

        public async Task<(double Performance, string Description)> TestParameters(Dictionary<string, object> parameters)
        {
            // 임시 전략 생성하여 테스트
            var tempStrategy = CreateImprovedStrategy();
            tempStrategy.UpdateParameters(parameters);

            var originalStrategy = _currentStrategy;
            _currentStrategy = tempStrategy;

            try
            {
                // 간단한 성과 추정 (실제로는 더 복잡한 백테스트 실행)
                var estimatedImprovement = EstimateImprovement(parameters);
                var description = GenerateParameterDescription(parameters);

                return await Task.FromResult((estimatedImprovement, description));
            }
            finally
            {
                _currentStrategy = originalStrategy;
            }
        }

        private TradingStrategy CreateImprovedStrategy()
        {
            // 현재 전략이 기본 MA20이면 개선된 버전으로 업그레이드
            if (_currentStrategy is MA20Strategy && !(_currentStrategy is EnhancedMA20Strategy))
            {
                return new EnhancedMA20Strategy();
            }

            // 이미 개선된 전략이면 AI 버전으로 업그레이드
            if (_currentStrategy is EnhancedMA20Strategy && !(_currentStrategy is AIEnhancedStrategy))
            {
                return new AIEnhancedStrategy();
            }

            // 이미 AI 전략이면 그대로 사용
            return new AIEnhancedStrategy();
        }

        private double EstimateImprovement(Dictionary<string, object> parameters)
        {
            double improvement = 0;

            // 각 파라미터별 예상 개선 효과
            if (parameters.ContainsKey("EnableVolatilityFilter") && (bool)parameters["EnableVolatilityFilter"])
                improvement += 0.15; // 15% 개선 예상

            if (parameters.ContainsKey("EnableTrendFilter") && (bool)parameters["EnableTrendFilter"])
                improvement += 0.10; // 10% 개선 예상

            if (parameters.ContainsKey("EnableConsecutiveLossFilter") && (bool)parameters["EnableConsecutiveLossFilter"])
                improvement += 0.08; // 8% 개선 예상

            return improvement;
        }

        private string GenerateParameterDescription(Dictionary<string, object> parameters)
        {
            var descriptions = new List<string>();

            if (parameters.ContainsKey("EnableVolatilityFilter") && (bool)parameters["EnableVolatilityFilter"])
                descriptions.Add("변동성 필터");

            if (parameters.ContainsKey("EnableTrendFilter") && (bool)parameters["EnableTrendFilter"])
                descriptions.Add("추세 필터");

            if (parameters.ContainsKey("EnableConsecutiveLossFilter") && (bool)parameters["EnableConsecutiveLossFilter"])
                descriptions.Add("연속손실 방지");

            return string.Join(" + ", descriptions);
        }

        private void CalculateMetrics(BacktestResult result)
        {
            if (!result.Trades.Any()) return;

            var wins = result.Trades.Where(t => t.ProfitLoss > 0);
            var losses = result.Trades.Where(t => t.ProfitLoss < 0);

            result.TotalTrades = result.Trades.Count;
            result.WinRate = (double)wins.Count() / result.TotalTrades;
            result.TotalReturn = result.Trades.Sum(t => t.ProfitLossPercent);
            result.AvgWin = wins.Any() ? wins.Average(t => t.ProfitLoss) : 0;
            result.AvgLoss = losses.Any() ? losses.Average(t => t.ProfitLoss) : 0;

            // 최대 낙폭 계산
            var cumulativePL = new List<double> { 0 };
            foreach (var trade in result.Trades)
            {
                cumulativePL.Add(cumulativePL.Last() + trade.ProfitLossPercent);
            }

            result.MaxDrawdown = 0;
            for (int i = 0; i < cumulativePL.Count; i++)
            {
                for (int j = i + 1; j < cumulativePL.Count; j++)
                {
                    var drawdown = cumulativePL[i] - cumulativePL[j];
                    if (drawdown > result.MaxDrawdown)
                        result.MaxDrawdown = drawdown;
                }
            }

            // 샤프 비율 계산 (간단 버전)
            if (result.Trades.Count > 1)
            {
                var returns = result.Trades.Select(t => t.ProfitLossPercent);
                var avgReturn = returns.Average();
                var stdDev = Math.Sqrt(returns.Sum(x => Math.Pow(x - avgReturn, 2)) / (result.Trades.Count - 1));
                result.SharpeRatio = stdDev > 0 ? avgReturn / stdDev : 0;
            }
        }
    }
}