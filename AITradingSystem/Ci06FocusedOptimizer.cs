using Binance.Net.Enums;
using Mercury.AITradingSystem.Models;
using Mercury.Backtests;
using Mercury.Charts;
using System.Text.Json;

namespace Mercury.AITradingSystem
{
    public class Ci06FocusedOptimizer
    {
        private readonly string _basePath;
        private readonly BacktestRunner _backtestRunner;
        private readonly ResultAnalyzer _resultAnalyzer;
        private readonly Random _random = new Random();
        private readonly Type _ci06StrategyType;

        // 목표 성능 지표
        public const decimal TARGET_RPR = 100m;
        public const decimal TARGET_WIN_RATE = 60m;

        public Ci06FocusedOptimizer(string basePath = "Ci06Optimization")
        {
            _basePath = basePath;
            _backtestRunner = new BacktestRunner(Path.Combine(basePath, "Backtests"));
            _resultAnalyzer = new ResultAnalyzer(Path.Combine(basePath, "Results"));
            _ci06StrategyType = typeof(Mercury.Backtests.BacktestStrategies.Ci06New);

            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(Path.Combine(basePath, "Backtests"));
            Directory.CreateDirectory(Path.Combine(basePath, "Results"));
            Directory.CreateDirectory(Path.Combine(basePath, "BestStrategies"));
        }

        public async Task RunOptimizationAsync(int maxIterations = 50)
        {
            Console.WriteLine("=== Ci06 Strategy Focused Optimization ===");
            Console.WriteLine($"Target RPR: {TARGET_RPR}, Target Win Rate: {TARGET_WIN_RATE:P1}");
            Console.WriteLine();

            // 이전 최고 전략 불러오기 시도
            var currentBestStrategy = LoadBestStrategy() ?? CreateInitialCi06Strategy();
            var bestResult = await TestStrategy(currentBestStrategy, 0);

            Console.WriteLine($"Initial Strategy Results:");
            Console.WriteLine($"  RPR: {bestResult.ResultPerRisk:F2}, Win Rate: {bestResult.WinRate:F2}%, ROI: {bestResult.Roe:P2}");
            Console.WriteLine();

            for (int iteration = 1; iteration <= maxIterations; iteration++)
            {
                Console.WriteLine($"=== Iteration {iteration} ===");

                // 현재 최고 전략 기반으로 개선된 전략들 생성
                var improvedStrategies = GenerateImprovedStrategies(currentBestStrategy, iteration);

                // 개선된 전략들 테스트
                var results = new List<BacktestResult>();
                foreach (var strategy in improvedStrategies)
                {
                    var result = await TestStrategy(strategy, iteration);
                    results.Add(result);
                }

                // 최고 결과 찾기
                var iterationBest = results
                    .Where(r => r.IsSuccess)
                    .OrderByDescending(r => r.ResultPerRisk)
                    .FirstOrDefault();

                if (iterationBest != null)
                {
                    Console.WriteLine($"Iteration Best: RPR {iterationBest.ResultPerRisk:F2}, Win Rate {iterationBest.WinRate:F2}%, ROI {iterationBest.Roe:P2}");

                    // 목표 도달 여부 확인
                    if (iterationBest.ResultPerRisk >= TARGET_RPR && iterationBest.WinRate >= TARGET_WIN_RATE)
                    {
                        Console.WriteLine($"🎉 TARGET ACHIEVED! RPR: {iterationBest.ResultPerRisk:F2}, Win Rate: {iterationBest.WinRate:F2}%");
                        await SaveBestStrategy(iterationBest, iteration);
                        break;
                    }

                    // 현재 최고 전략보다 개선되었으면 업데이트
                    if (iterationBest.ResultPerRisk > bestResult.ResultPerRisk)
                    {
                        bestResult = iterationBest;
                        currentBestStrategy = CreateStrategyFromResult(iterationBest);
                        Console.WriteLine($"New best strategy found! RPR improved from {bestResult.ResultPerRisk:F2}");
                        await SaveBestStrategy(iterationBest, iteration);
                    }
                }
                else
                {
                    Console.WriteLine("No successful strategies in this iteration.");
                }

                Console.WriteLine($"Current Best: RPR {bestResult.ResultPerRisk:F2}, Win Rate {bestResult.WinRate:F2}%");
                Console.WriteLine();

                // 진행 상황 저장
                await SaveProgress(iteration, bestResult, results);
            }

            Console.WriteLine("=== Optimization Complete ===");
            Console.WriteLine($"Best RPR Achieved: {bestResult.ResultPerRisk:F2}");
            Console.WriteLine($"Best Win Rate: {bestResult.WinRate:F2}%");
            Console.WriteLine($"Best ROI: {bestResult.Roe:P2}");
        }

        private StrategyInfo CreateInitialCi06Strategy()
        {
            return new StrategyInfo
            {
                Name = "Ci06_Initial",
                ClassName = "Ci06",
                StrategyType = "Ci06",
                Description = "Initial Ci06New strategy with CCI and Ichimoku Cloud",
                Generation = 0,
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = _ci06StrategyType,
                Parameters = new Dictionary<string, object>
                {
                    ["CciPeriod"] = 14,
                    ["EntryCciLong"] = -150m,
                    ["EntryCciShort"] = 150m,
                    ["ExitCciLong"] = 100m,
                    ["ExitCciShort"] = -100m,
                    ["IchimokuConversionPeriod"] = 9,
                    ["IchimokuBasePeriod"] = 26,
                    ["IchimokuLeadingSpanPeriod"] = 52,
                    ["UseTrendConfirmation"] = true,
                    ["VolumeThreshold"] = 1.2m,
                    ["ConfirmationCandles"] = 1
                }
            };
        }

        private List<StrategyInfo> GenerateImprovedStrategies(StrategyInfo baseStrategy, int generation)
        {
            var strategies = new List<StrategyInfo>();

            // 1. 파라미터 미세 조정 variations
            for (int i = 0; i < 5; i++)
            {
                var strategy = MutateParameters(baseStrategy, generation, i);
                strategies.Add(strategy);
            }

            // 2. RPR 개선을 위한 특화 variations
            strategies.Add(CreateRprFocusedVariant(baseStrategy, generation));

            // 3. Win Rate 개선을 위한 특화 variations
            strategies.Add(CreateWinRateFocusedVariant(baseStrategy, generation));

            // 4. 밸런스드 variant
            strategies.Add(CreateBalancedVariant(baseStrategy, generation));

            return strategies;
        }

        private StrategyInfo MutateParameters(StrategyInfo baseStrategy, int generation, int variantIndex)
        {
            var mutatedParams = new Dictionary<string, object>(baseStrategy.Parameters);
            var seed = variantIndex * 100 + generation;
            var localRandom = new Random(seed);

            // CCI Period 조정
            if (mutatedParams["CciPeriod"] is int cciPeriod)
            {
                mutatedParams["CciPeriod"] = Math.Max(10, Math.Min(30, cciPeriod + localRandom.Next(-3, 4)));
            }

            // Ichimoku 파라미터 조정
            if (mutatedParams["IchimokuConversionPeriod"] is int conversionPeriod)
            {
                mutatedParams["IchimokuConversionPeriod"] = Math.Max(6, Math.Min(15, conversionPeriod + localRandom.Next(-2, 3)));
            }
            if (mutatedParams["IchimokuBasePeriod"] is int basePeriod)
            {
                mutatedParams["IchimokuBasePeriod"] = Math.Max(20, Math.Min(40, basePeriod + localRandom.Next(-3, 4)));
            }
            if (mutatedParams["IchimokuLeadingSpanPeriod"] is int leadingSpanPeriod)
            {
                mutatedParams["IchimokuLeadingSpanPeriod"] = Math.Max(40, Math.Min(60, leadingSpanPeriod + localRandom.Next(-5, 6)));
            }

            // CCI Entry/Exit Level 조정
            if (mutatedParams["EntryCciLong"] is decimal entryCciLong)
            {
                var adjustment = (decimal)(localRandom.NextDouble() - 0.5) * 30m;
                mutatedParams["EntryCciLong"] = Math.Max(-200m, Math.Min(-100m, entryCciLong + adjustment));
            }
            if (mutatedParams["EntryCciShort"] is decimal entryCciShort)
            {
                var adjustment = (decimal)(localRandom.NextDouble() - 0.5) * 30m;
                mutatedParams["EntryCciShort"] = Math.Max(100m, Math.Min(200m, entryCciShort + adjustment));
            }
            if (mutatedParams["ExitCciLong"] is decimal exitCciLong)
            {
                var adjustment = (decimal)(localRandom.NextDouble() - 0.5) * 20m;
                mutatedParams["ExitCciLong"] = Math.Max(50m, Math.Min(150m, exitCciLong + adjustment));
            }
            if (mutatedParams["ExitCciShort"] is decimal exitCciShort)
            {
                var adjustment = (decimal)(localRandom.NextDouble() - 0.5) * 20m;
                mutatedParams["ExitCciShort"] = Math.Max(-150m, Math.Min(-50m, exitCciShort + adjustment));
            }

            return new StrategyInfo
            {
                Name = $"Ci06_Gen{generation}_V{variantIndex}",
                ClassName = "Ci06",
                StrategyType = "Ci06",
                Description = $"Parameter mutation variant {variantIndex}",
                Generation = generation,
                ParentStrategies = new List<string> { baseStrategy.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = _ci06StrategyType,
                Parameters = mutatedParams
            };
        }

        private StrategyInfo CreateRprFocusedVariant(StrategyInfo baseStrategy, int generation)
        {
            var rprParams = new Dictionary<string, object>(baseStrategy.Parameters);

            // RPR을 높이기 위해 더 엄격한 진입 조건과 빠른 청산
            rprParams["EntryCciLong"] = -180m; // 더 엄격한 롱 진입 조건
            rprParams["EntryCciShort"] = 180m; // 더 엄격한 숏 진입 조건
            rprParams["ExitCciLong"] = 80m; // 더 빠른 롱 청산
            rprParams["ExitCciShort"] = -80m; // 더 빠른 숏 청산
            rprParams["CciPeriod"] = 18; // 약간 더 긴 CCI 기간으로 신뢰도 증가
            rprParams["IchimokuConversionPeriod"] = 8; // 더 빠른 반응
            rprParams["IchimokuBasePeriod"] = 30; // 더 안정적인 기준선

            return new StrategyInfo
            {
                Name = $"Ci06_RprFocus_Gen{generation}",
                ClassName = "Ci06",
                StrategyType = "Ci06",
                Description = "RPR focused variant with stricter entry conditions",
                Generation = generation,
                ParentStrategies = new List<string> { baseStrategy.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = _ci06StrategyType,
                Parameters = rprParams
            };
        }

        private StrategyInfo CreateWinRateFocusedVariant(StrategyInfo baseStrategy, int generation)
        {
            var winRateParams = new Dictionary<string, object>(baseStrategy.Parameters);

            // 승률을 높이기 위해 보수적인 접근
            winRateParams["EntryCciLong"] = -120m; // 덜 엄격한 롱 진입 조건
            winRateParams["EntryCciShort"] = 120m; // 덜 엄격한 숏 진입 조건
            winRateParams["ExitCciLong"] = 120m; // 더 보수적인 롱 청산
            winRateParams["ExitCciShort"] = -120m; // 더 보수적인 숏 청산
            winRateParams["CciPeriod"] = 12; // 더 짧은 기간으로 빠른 반응
            winRateParams["IchimokuConversionPeriod"] = 10; // 표준적인 값
            winRateParams["IchimokuBasePeriod"] = 24; // 표준적인 값
            winRateParams["IchimokuLeadingSpanPeriod"] = 48; // 표준적인 값

            return new StrategyInfo
            {
                Name = $"Ci06_WinRateFocus_Gen{generation}",
                ClassName = "Ci06",
                StrategyType = "Ci06",
                Description = "Win rate focused variant with conservative approach",
                Generation = generation,
                ParentStrategies = new List<string> { baseStrategy.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = _ci06StrategyType,
                Parameters = winRateParams
            };
        }

        private StrategyInfo CreateBalancedVariant(StrategyInfo baseStrategy, int generation)
        {
            var balancedParams = new Dictionary<string, object>(baseStrategy.Parameters);

            // RPR과 승률의 밸런스
            balancedParams["EntryCciLong"] = -140m; // 중간 롱 진입 조건
            balancedParams["EntryCciShort"] = 140m; // 중간 숏 진입 조건
            balancedParams["ExitCciLong"] = 110m; // 중간 롱 청산 조건
            balancedParams["ExitCciShort"] = -110m; // 중간 숏 청산 조건
            balancedParams["CciPeriod"] = 16; // 중간 기간
            balancedParams["IchimokuConversionPeriod"] = 9; // 균형 잡힌 값
            balancedParams["IchimokuBasePeriod"] = 26; // 표준 값
            balancedParams["IchimokuLeadingSpanPeriod"] = 52; // 표준 값

            return new StrategyInfo
            {
                Name = $"Ci06_Balanced_Gen{generation}",
                ClassName = "Ci06",
                StrategyType = "Ci06",
                Description = "Balanced variant for optimal RPR and win rate",
                Generation = generation,
                ParentStrategies = new List<string> { baseStrategy.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = _ci06StrategyType,
                Parameters = balancedParams
            };
        }

        private async Task<BacktestResult> TestStrategy(StrategyInfo strategy, int iteration)
        {
            try
            {
                var results = await _backtestRunner.RunBacktestsAsync(new List<StrategyInfo> { strategy }, iteration);
                var successfulResults = results.Where(r => r.IsSuccess).ToList();

                if (!successfulResults.Any())
                {
                    return results.First() ?? new BacktestResult
                    {
                        StrategyName = strategy.Name,
                        IsSuccess = false,
                        Error = "No successful results"
                    };
                }

                // 모든 심볼의 결과 평균 계산
                var avgResult = new BacktestResult
                {
                    StrategyName = strategy.Name,
                    Parameters = strategy.Parameters,
                    IsSuccess = true,
                    Roe = successfulResults.Average(r => r.Roe),
                    WinRate = successfulResults.Average(r => r.WinRate),
                    Mdd = successfulResults.Average(r => r.Mdd),
                    ResultPerRisk = successfulResults.Average(r => r.ResultPerRisk),
                    Win = successfulResults.Sum(r => r.Win),
                    Lose = successfulResults.Sum(r => r.Lose),
                    FinalMoney = successfulResults.Average(r => r.FinalMoney),
                    RunTime = TimeSpan.FromTicks((long)successfulResults.Average(r => r.RunTime?.Ticks ?? 0))
                };

                return avgResult;
            }
            catch (Exception ex)
            {
                return new BacktestResult
                {
                    StrategyName = strategy.Name,
                    IsSuccess = false,
                    Error = ex.Message
                };
            }
        }

        private StrategyInfo CreateStrategyFromResult(BacktestResult result)
        {
            return new StrategyInfo
            {
                Name = result.StrategyName,
                ClassName = "Ci06",
                StrategyType = "Ci06",
                Parameters = result.Parameters,
                AverageRoe = result.Roe,
                AverageWinRate = result.WinRate,
                AverageMdd = result.Mdd,
                AverageResultPerRisk = result.ResultPerRisk,
                TotalTrades = result.Win + result.Lose,
                StrategyRuntimeType = _ci06StrategyType
            };
        }

        private async Task SaveBestStrategy(BacktestResult result, int iteration)
        {
            var bestStrategyPath = Path.Combine(_basePath, "BestStrategies", $"best_strategy_iteration_{iteration}.json");
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(bestStrategyPath, json);

            Console.WriteLine($"Best strategy saved to: {bestStrategyPath}");
        }

    private async Task SaveProgress(int iteration, BacktestResult bestResult, List<BacktestResult> iterationResults)
        {
            var progress = new
            {
                Iteration = iteration,
                BestResult = bestResult,
                AllResults = iterationResults,
                Timestamp = DateTime.UtcNow
            };

            var progressPath = Path.Combine(_basePath, "Results", $"progress_iteration_{iteration}.json");
            var json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(progressPath, json);
        }

        private StrategyInfo? LoadBestStrategy()
        {
            try
            {
                var bestStrategiesPath = Path.Combine(_basePath, "BestStrategies");
                if (!Directory.Exists(bestStrategiesPath))
                    return null;

                // 가장 최신의 best strategy 파일 찾기
                var files = Directory.GetFiles(bestStrategiesPath, "best_strategy_iteration_*.json")
                    .OrderByDescending(f => f)
                    .ToList();

                if (!files.Any())
                    return null;

                var latestFile = files.First();
                var json = File.ReadAllText(latestFile);
                var bestResult = JsonSerializer.Deserialize<BacktestResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (bestResult == null || bestResult.Parameters == null)
                    return null;

                Console.WriteLine($"Loaded previous best strategy: {bestResult.StrategyName} (RPR: {bestResult.ResultPerRisk:F2}, Win Rate: {bestResult.WinRate:F2}%)");

                return new StrategyInfo
                {
                    Name = bestResult.StrategyName + "_Continued",
                    ClassName = "Ci06",
                    StrategyType = "Ci06",
                    Description = "Continued from previous best strategy",
                    Generation = 0, // 새로운 시작으로 리셋
                    Parameters = new Dictionary<string, object>(bestResult.Parameters),
                    StrategyRuntimeType = _ci06StrategyType,
                    AverageRoe = bestResult.Roe,
                    AverageWinRate = bestResult.WinRate,
                    AverageMdd = bestResult.Mdd,
                    AverageResultPerRisk = bestResult.ResultPerRisk,
                    TotalTrades = bestResult.Win + bestResult.Lose
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load previous best strategy: {ex.Message}");
                return null;
            }
        }
    }
}