using AITradingSystem.Models;

namespace AITradingSystem.Services
{
    public class StrategyAnalyzer
    {
        public AnalysisResult AnalyzeBacktestResult(BacktestResult result, List<MarketData> marketData)
        {
            var analysis = new AnalysisResult();

            // 1. 손실 거래 패턴 분석
            var losingTrades = result.Trades.Where(t => t.ProfitLoss < 0).ToList();
            analysis.Weaknesses = AnalyzeLosses(losingTrades, marketData);

            // 2. 시장 조건별 성과 분석
            analysis.MarketConditionPerformance = AnalyzeMarketConditions(result.Trades);

            // 3. 개선 제안 생성
            analysis.ImprovementSuggestions = GenerateImprovements(analysis);

            return analysis;
        }

        private List<Weakness> AnalyzeLosses(List<Trade> losingTrades, List<MarketData> marketData)
        {
            var weaknesses = new List<Weakness>();

            // 연속 손실 패턴
            var consecutiveLosses = FindConsecutiveLosses(losingTrades);
            if (consecutiveLosses.Count > 3)
            {
                weaknesses.Add(new Weakness
                {
                    Type = "ConsecutiveLosses",
                    Description = $"{consecutiveLosses.Count}번 연속 손실 발생",
                    Impact = consecutiveLosses.Sum(t => Math.Abs(t.ProfitLoss)),
                    Suggestion = "추세 필터 또는 시장 조건 필터 추가 고려"
                });
            }

            // 변동성이 높은 시점에서의 손실
            var highVolatilityLosses = losingTrades.Where(t =>
                t.MarketCondition.ContainsKey("Volatility") &&
                (double)t.MarketCondition["Volatility"] > 0.02).ToList();

            if (highVolatilityLosses.Count > losingTrades.Count * 0.6)
            {
                weaknesses.Add(new Weakness
                {
                    Type = "HighVolatilityLosses",
                    Description = "높은 변동성 구간에서 과도한 손실",
                    Impact = highVolatilityLosses.Sum(t => Math.Abs(t.ProfitLoss)),
                    Suggestion = "변동성 필터 추가 - 변동성 > 임계값일 때 거래 중단"
                });
            }

            return weaknesses;
        }

        private Dictionary<string, double> AnalyzeMarketConditions(List<Trade> trades)
        {
            var conditions = new Dictionary<string, double>();

            // 추세 강도별 성과
            var strongTrendTrades = trades.Where(t =>
                t.MarketCondition.ContainsKey("TrendStrength") &&
                (double)t.MarketCondition["TrendStrength"] > 0.7).ToList();

            conditions["StrongTrendPerformance"] = strongTrendTrades.Any() ?
                strongTrendTrades.Average(t => t.ProfitLoss) : 0;

            // 변동성별 성과
            var lowVolTrades = trades.Where(t =>
                t.MarketCondition.ContainsKey("Volatility") &&
                (double)t.MarketCondition["Volatility"] < 0.01).ToList();

            conditions["LowVolatilityPerformance"] = lowVolTrades.Any() ?
                lowVolTrades.Average(t => t.ProfitLoss) : 0;

            return conditions;
        }

        private List<ImprovementSuggestion> GenerateImprovements(AnalysisResult analysis)
        {
            var suggestions = new List<ImprovementSuggestion>();

            foreach (var weakness in analysis.Weaknesses)
            {
                switch (weakness.Type)
                {
                    case "HighVolatilityLosses":
                        suggestions.Add(new ImprovementSuggestion
                        {
                            Type = "AddVolatilityFilter",
                            Description = "변동성 필터 추가",
                            NewParameters = new Dictionary<string, object>
                            {
                                ["EnableVolatilityFilter"] = true,
                                ["MaxVolatility"] = 0.015
                            },
                            ExpectedImprovement = weakness.Impact * 0.7
                        });
                        break;

                    case "ConsecutiveLosses":
                        suggestions.Add(new ImprovementSuggestion
                        {
                            Type = "AddTrendFilter",
                            Description = "추세 필터 추가",
                            NewParameters = new Dictionary<string, object>
                            {
                                ["EnableTrendFilter"] = true,
                                ["TrendPeriod"] = 50,
                                ["MinTrendStrength"] = 0.6
                            },
                            ExpectedImprovement = weakness.Impact * 0.5
                        });
                        break;
                }
            }

            return suggestions;
        }

        private List<Trade> FindConsecutiveLosses(List<Trade> losingTrades)
        {
            // 시간순 정렬 후 연속 손실 찾기
            return losingTrades.OrderBy(t => t.EntryTime).ToList();
        }
    }
}
