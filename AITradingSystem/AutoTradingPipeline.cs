using Mercury.Charts;
using Mercury.Enums;
using Mercury.AITradingSystem.Models;
using System.Text.Json;
using System.IO;

namespace Mercury.AITradingSystem
{
    public class AutoTradingPipeline
    {
        private readonly string _basePath;
        private readonly StrategyGenerator _strategyGenerator;
        private readonly BacktestRunner _backtestRunner;
        private readonly ResultAnalyzer _resultAnalyzer;
        private readonly StrategyImprover _strategyImprover;

        public AutoTradingPipeline(string basePath = "AITradingSystem")
        {
            _basePath = basePath;
            _strategyGenerator = new StrategyGenerator(Path.Combine(basePath, "Strategies"));
            _backtestRunner = new BacktestRunner(Path.Combine(basePath, "Backtests"));
            _resultAnalyzer = new ResultAnalyzer(Path.Combine(basePath, "Results"));
            _strategyImprover = new StrategyImprover(Path.Combine(basePath, "Improvements"));

            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(Path.Combine(basePath, "Strategies"));
            Directory.CreateDirectory(Path.Combine(basePath, "Backtests"));
            Directory.CreateDirectory(Path.Combine(basePath, "Results"));
            Directory.CreateDirectory(Path.Combine(basePath, "Improvements"));
        }

        public async Task RunContinuousOptimizationAsync(int maxIterations = 10, CancellationToken cancellationToken = default)
        {
            var iteration = 0;
            var currentStrategySet = new List<StrategyInfo>();

            // 초기 전략 집합 생성
            Console.WriteLine("Generating initial strategy set...");
            currentStrategySet = await _strategyGenerator.GenerateInitialStrategySetAsync();

            while (iteration < maxIterations && !cancellationToken.IsCancellationRequested)
            {
                iteration++;
                Console.WriteLine($"=== Iteration {iteration} ===");

                // 1. 현재 전략 집합으로 백테스팅 실행
                Console.WriteLine($"Running backtests for {currentStrategySet.Count} strategies...");
                var backtestResults = await _backtestRunner.RunBacktestsAsync(currentStrategySet, iteration);

                // 2. 결과 분석
                Console.WriteLine("Analyzing results...");
                var analysisResult = _resultAnalyzer.AnalyzeResults(backtestResults);

                // 3. 최상위 전략 선택
                var topStrategies = _resultAnalyzer.GetTopStrategies(analysisResult, 5);

                // 4. 전략 개선 및 새로운 전략 생성
                Console.WriteLine("Improving strategies and generating new variants...");
                var improvedStrategies = await _strategyImprover.ImproveStrategiesAsync(topStrategies, analysisResult);

                // 5. 다음 세대 전략 집합 준비
                currentStrategySet = improvedStrategies.Concat(topStrategies).Take(20).ToList();

                // 6. 결과 저장
                await SaveIterationResultsAsync(iteration, analysisResult, currentStrategySet);

                Console.WriteLine($"Iteration {iteration} completed. Top strategy ROI: {topStrategies.FirstOrDefault()?.AverageRoe:P2}");

                // 최적화 조건 확인
                if (ShouldStopOptimization(analysisResult))
                {
                    Console.WriteLine("Optimization converged. Stopping...");
                    break;
                }

                await Task.Delay(1000, cancellationToken); // 잠시 대기
            }
        }

        private async Task SaveIterationResultsAsync(int iteration, AnalysisResult analysis, List<StrategyInfo> strategies)
        {
            var resultPath = Path.Combine(_basePath, "Results", $"iteration_{iteration}.json");
            var resultData = new
            {
                Iteration = iteration,
                Timestamp = DateTime.UtcNow,
                TopStrategies = analysis.TopStrategies.Take(10).Select(s => new
                {
                    s.Name,
                    s.AverageRoe,
                    s.AverageWinRate,
                    s.AverageMdd,
                    s.Parameters
                }),
                Statistics = new
                {
                    TotalStrategies = analysis.TotalStrategies,
                    ViableStrategies = analysis.ViableStrategies,
                    AverageRoe = analysis.AverageRoe,
                    AverageWinRate = analysis.AverageWinRate,
                    AverageMdd = analysis.AverageMdd
                }
            };

            await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(resultData, new JsonSerializerOptions { WriteIndented = true }));
        }

        private bool ShouldStopOptimization(AnalysisResult analysis)
        {
            // ROI가 30% 이상이고 승률이 60% 이상인 전략이 있으면 중단
            return analysis.TopStrategies.Any(s => s.AverageRoe >= 0.3m && s.AverageWinRate >= 0.6m);
        }
    }
}