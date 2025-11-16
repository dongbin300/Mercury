using Mercury.AITradingSystem.Models;
using Mercury.Charts;
using Binance.Net.Enums;

namespace Mercury.AITradingSystem
{
    public class TestRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== AI Trading System Test ===");

            try
            {
                // 1. 시스템 초기화 테스트
                await TestSystemInitialization();

                // 2. 전략 생성 테스트
                await TestStrategyGeneration();

                // 3. 간단한 백테스트 테스트
                await TestSimpleBacktest();

                Console.WriteLine("All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestSystemInitialization()
        {
            Console.WriteLine("\n1. Testing system initialization...");

            var basePath = "AITradingSystem";
            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(Path.Combine(basePath, "Strategies"));
            Directory.CreateDirectory(Path.Combine(basePath, "Backtests"));
            Directory.CreateDirectory(Path.Combine(basePath, "Results"));

            Console.WriteLine("✓ Directory structure created");
        }

        private static async Task TestStrategyGeneration()
        {
            Console.WriteLine("\n2. Testing strategy generation...");

            var strategyGenerator = new StrategyGenerator(Path.Combine("AITradingSystem", "Strategies"));
            var strategies = await strategyGenerator.GenerateInitialStrategySetAsync();

            Console.WriteLine($"✓ Generated {strategies.Count} strategies");

            foreach (var strategy in strategies.Take(3))
            {
                Console.WriteLine($"  - {strategy.Name} ({strategy.StrategyType}): {string.Join(", ", strategy.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            }
        }

        private static async Task TestSimpleBacktest()
        {
            Console.WriteLine("\n3. Testing simple backtest...");

            var backtestRunner = new BacktestRunner(Path.Combine("AITradingSystem", "Backtests"));

            // 간단한 CCI 전략 생성
            var testStrategy = new StrategyInfo
            {
                Name = "TestCCIStrategy",
                ClassName = "TestCCI",
                StrategyType = "CCI",
                Parameters = new Dictionary<string, object>
                {
                    ["EntryCci"] = -150,
                    ["ExitCci"] = 200,
                    ["CciPeriod"] = 20
                },
                StrategyRuntimeType = typeof(Mercury.Backtests.BacktestStrategies.Cci1)
            };

            var results = await backtestRunner.RunBacktestsAsync(new List<StrategyInfo> { testStrategy }, 1);

            Console.WriteLine($"✓ Completed {results.Count} backtest(s)");

            if (results.Any())
            {
                var result = results.First();
                Console.WriteLine($"  - Strategy: {result.StrategyName}");
                Console.WriteLine($"  - Symbol: {result.Symbol}");
                Console.WriteLine($"  - ROI: {result.Roe:P2}");
                Console.WriteLine($"  - Win Rate: {result.WinRate:P2}");
                Console.WriteLine($"  - Success: {result.IsSuccess}");

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"  - Error: {result.Error}");
                }
            }
        }
    }
}