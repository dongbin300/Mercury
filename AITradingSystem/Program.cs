using Mercury.AITradingSystem;
using System.Threading;

namespace Mercury.AITradingSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== AI Trading Strategy Automation System ===");
            Console.WriteLine("This system will automatically generate, test, and improve trading strategies.");
            Console.WriteLine();

            try
            {
                // 설정
                var maxIterations = 10;
                var basePath = "AITradingSystem";

                if (args.Length > 0 && int.TryParse(args[0], out var iterations))
                {
                    maxIterations = iterations;
                }

                Console.WriteLine($"Configuration:");
                Console.WriteLine($"- Max Iterations: {maxIterations}");
                Console.WriteLine($"- Base Path: {basePath}");
                Console.WriteLine($"- Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();

                // Ci06 최적화기 초기화
                var optimizer = new Ci06FocusedOptimizer(basePath);

                // 취소 토큰 설정
                var cts = new CancellationTokenSource();

                // Ctrl+C 핸들러
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\nShutdown requested. Stopping gracefully...");
                    cts.Cancel();
                };

                // Ci06 전략 집중 최적화 실행
                await optimizer.RunOptimizationAsync(maxIterations);

                Console.WriteLine("\n=== Automation Complete ===");
                Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Results saved in: {Path.GetFullPath(basePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Environment.ExitCode = 1;
            }

            Console.WriteLine("\nProgram completed. Exiting automatically...");
        }
    }
}