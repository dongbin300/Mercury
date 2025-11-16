using Mercury.AITradingSystem.Models;
using System.Text.Json;

namespace Mercury.AITradingSystem
{
    public class ResultAnalyzer
    {
        private readonly string _resultsPath;
        private readonly Dictionary<string, decimal> _parameterWeights;

        public ResultAnalyzer(string resultsPath)
        {
            _resultsPath = resultsPath;
            _parameterWeights = InitializeParameterWeights();
        }

        public AnalysisResult AnalyzeResults(List<BacktestResult> results)
        {
            Console.WriteLine($"Analyzing {results.Count} backtest results...");

            var analysis = new AnalysisResult
            {
                AllResults = results,
                TotalStrategies = results.Select(r => r.StrategyName).Distinct().Count(),
                AnalysisTime = DateTime.UtcNow
            };

            // 성공적인 결과만 필터링
            var successfulResults = results.Where(r => r.IsSuccess && r.Roe > 0).ToList();
            analysis.ViableStrategies = successfulResults.Select(r => r.StrategyName).Distinct().Count();

            if (successfulResults.Any())
            {
                // 기본 통계 계산
                analysis.AverageRoe = successfulResults.Average(r => r.Roe);
                analysis.AverageWinRate = successfulResults.Average(r => r.WinRate);
                analysis.AverageMdd = successfulResults.Average(r => r.Mdd);

                // 전략별 집계
                var strategyGroups = successfulResults
                    .GroupBy(r => r.StrategyName)
                    .Select(g => new StrategyInfo
                    {
                        Name = g.Key,
                        IndividualResults = g.ToList(),
                        AverageRoe = g.Average(r => r.Roe),
                        AverageWinRate = g.Average(r => r.WinRate),
                        AverageMdd = g.Average(r => r.Mdd),
                        AverageResultPerRisk = g.Average(r => r.ResultPerRisk),
                        TotalTrades = g.Sum(r => r.Win + r.Lose),
                        Parameters = g.First().Parameters // 모든 결과에서 파라미터는 동일하다고 가정
                    })
                    .OrderByDescending(s => s.AverageResultPerRisk) // 리스크 대비 수익률로 정렬
                    .ToList();

                analysis.TopStrategies = strategyGroups;

                // 파라미터 중요도 분석
                analysis.ParameterImportance = AnalyzeParameterImportance(successfulResults);

                // 개선 추천 생성
                analysis.RecommendedImprovements = GenerateImprovementRecommendations(analysis);

                Console.WriteLine($"Analysis completed. Found {analysis.TopStrategies.Count} viable strategies.");
                Console.WriteLine($"Average ROI: {analysis.AverageRoe:P2}, Win Rate: {analysis.AverageWinRate:P2}, MDD: {analysis.AverageMdd:P2}");
            }
            else
            {
                Console.WriteLine("No successful results found for analysis.");
            }

            return analysis;
        }

        public List<StrategyInfo> GetTopStrategies(AnalysisResult analysis, int count)
        {
            return analysis.TopStrategies.Take(count).ToList();
        }

        private Dictionary<string, decimal> AnalyzeParameterImportance(List<BacktestResult> results)
        {
            var parameterImportance = new Dictionary<string, decimal>();

            if (!results.Any()) return parameterImportance;

            // 모든 파라미터 이름 수집
            var allParameters = results
                .SelectMany(r => r.Parameters.Keys)
                .Distinct()
                .ToList();

            foreach (var paramName in allParameters)
            {
                var importance = CalculateParameterImportance(results, paramName);
                parameterImportance[paramName] = importance;
            }

            return parameterImportance;
        }

        private decimal CalculateParameterImportance(List<BacktestResult> results, string paramName)
        {
            // 파라미터 값과 ROI 간의 상관관계 계산
            var paramValues = results
                .Where(r => r.Parameters.ContainsKey(paramName) && r.IsSuccess)
                .Select(r => new
                {
                    ParamValue = Convert.ToDecimal(r.Parameters[paramName]),
                    Roe = r.Roe
                })
                .ToList();

            if (paramValues.Count < 3) return 0m;

            // 상관관계 계산
            var avgParam = paramValues.Average(p => p.ParamValue);
            var avgRoe = paramValues.Average(p => p.Roe);

            var numerator = paramValues.Sum(p => (p.ParamValue - avgParam) * (p.Roe - avgRoe));
            var paramVariance = paramValues.Sum(p => Math.Pow((double)(p.ParamValue - avgParam), 2));
            var roeVariance = paramValues.Sum(p => Math.Pow((double)(p.Roe - avgRoe), 2));

            if (paramVariance == 0 || roeVariance == 0) return 0m;

            var correlation = numerator / (decimal)Math.Sqrt((double)paramVariance * (double)roeVariance);
            return Math.Abs(correlation); // 절대값 반환 (상관관계의 강도)
        }

        private List<string> GenerateImprovementRecommendations(AnalysisResult analysis)
        {
            var recommendations = new List<string>();

            if (!analysis.TopStrategies.Any())
            {
                recommendations.Add("No viable strategies found. Consider adjusting entry/exit conditions.");
                return recommendations;
            }

            var topStrategy = analysis.TopStrategies.First();

            // ROI 기반 추천
            if (analysis.AverageRoe < 0.1m)
            {
                recommendations.Add("Overall ROI is low. Consider tightening entry conditions or improving exit logic.");
            }

            // 승률 기반 추천
            if (analysis.AverageWinRate < 0.5m)
            {
                recommendations.Add("Win rate is below 50%. Consider adding trend confirmation indicators.");
            }

            // MDD 기반 추천
            if (analysis.AverageMdd > 0.2m)
            {
                recommendations.Add("Maximum drawdown is high. Consider implementing better stop-loss mechanisms.");
            }

            // 파라미터 기반 추천
            var mostImportantParam = analysis.ParameterImportance
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(mostImportantParam.Key))
            {
                recommendations.Add($"Parameter '{mostImportantParam.Key}' shows highest correlation with performance. Focus optimization on this parameter.");
            }

            // 전략 타입별 추천
            var strategyTypePerformance = analysis.TopStrategies
                .GroupBy(s => s.Name.Split("Strategy")[0])
                .Select(g => new
                {
                    Type = g.Key,
                    AvgRoe = g.Average(s => s.AverageRoe),
                    Count = g.Count()
                })
                .OrderByDescending(t => t.AvgRoe)
                .FirstOrDefault();

            if (strategyTypePerformance != null)
            {
                recommendations.Add($"Strategy type '{strategyTypePerformance.Type}' shows best performance. Consider generating more variants of this type.");
            }

            // 구체적인 개선 아이디어
            if (topStrategy.AverageMdd > 0.15m)
            {
                recommendations.Add("Consider adding volume-based confirmation to reduce false signals.");
            }

            if (topStrategy.AverageWinRate < 0.6m)
            {
                recommendations.Add("Consider implementing multi-timeframe analysis for better entry timing.");
            }

            return recommendations;
        }

        public async Task SaveAnalysisAsync(AnalysisResult analysis, int iteration)
        {
            var analysisPath = Path.Combine(_resultsPath, $"analysis_iteration_{iteration}.json");
            var json = JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(analysisPath, json);

            // 요약 보고서 생성
            var reportPath = Path.Combine(_resultsPath, $"report_iteration_{iteration}.txt");
            var report = GenerateTextReport(analysis);
            await File.WriteAllTextAsync(reportPath, report);
        }

        private string GenerateTextReport(AnalysisResult analysis)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine($"=== AI Trading Strategy Analysis Report ===");
            report.AppendLine($"Generated: {analysis.AnalysisTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            report.AppendLine("=== Overall Statistics ===");
            report.AppendLine($"Total Strategies Tested: {analysis.TotalStrategies}");
            report.AppendLine($"Viable Strategies: {analysis.ViableStrategies}");
            report.AppendLine($"Average ROI: {analysis.AverageRoe:P2}");
            report.AppendLine($"Average Win Rate: {analysis.AverageWinRate:P2}");
            report.AppendLine($"Average MDD: {analysis.AverageMdd:P2}");
            report.AppendLine();

            report.AppendLine("=== Top 5 Strategies ===");
            for (int i = 0; i < Math.Min(5, analysis.TopStrategies.Count); i++)
            {
                var strategy = analysis.TopStrategies[i];
                report.AppendLine($"{i + 1}. {strategy.Name}");
                report.AppendLine($"   ROI: {strategy.AverageRoe:P2}");
                report.AppendLine($"   Win Rate: {strategy.AverageWinRate:P2}");
                report.AppendLine($"   MDD: {strategy.AverageMdd:P2}");
                report.AppendLine($"   Risk/Reward: {strategy.AverageResultPerRisk:F2}");
                report.AppendLine($"   Total Trades: {strategy.TotalTrades}");
                report.AppendLine();
            }

            report.AppendLine("=== Parameter Importance ===");
            foreach (var param in analysis.ParameterImportance.OrderByDescending(kvp => kvp.Value).Take(5))
            {
                report.AppendLine($"{param.Key}: {param.Value:F3}");
            }
            report.AppendLine();

            report.AppendLine("=== Improvement Recommendations ===");
            foreach (var recommendation in analysis.RecommendedImprovements)
            {
                report.AppendLine($"• {recommendation}");
            }

            return report.ToString();
        }

        private Dictionary<string, decimal> InitializeParameterWeights()
        {
            // 파라미터별 기본 가중치 설정
            return new Dictionary<string, decimal>
            {
                ["EntryCci"] = 0.8m,
                ["ExitCci"] = 0.7m,
                ["CciPeriod"] = 0.6m,
                ["EmaPeriod"] = 0.5m,
                ["StopLoss"] = 0.9m,
                ["TakeProfit"] = 0.8m,
                ["VolumeThreshold"] = 0.4m
            };
        }

        public AnalysisResult LoadPreviousAnalysis(int iteration)
        {
            var analysisPath = Path.Combine(_resultsPath, $"analysis_iteration_{iteration}.json");
            if (!File.Exists(analysisPath))
            {
                return new AnalysisResult();
            }

            var json = File.ReadAllText(analysisPath);
            return JsonSerializer.Deserialize<AnalysisResult>(json) ?? new AnalysisResult();
        }
    }
}