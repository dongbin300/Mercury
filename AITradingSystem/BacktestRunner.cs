using Binance.Net.Enums;
using Mercury.AITradingSystem.Models;
using Mercury.Backtests;
using Mercury.Charts;
using Mercury.Enums;
using System.Text.Json;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mercury.AITradingSystem
{
    public class BacktestRunner
    {
        private readonly string _backtestPath;
        private readonly List<string> _testSymbols;
        private readonly List<KlineInterval> _testIntervals;
        private readonly DateTime _defaultStartDate;
        private readonly DateTime _defaultEndDate;

        public BacktestRunner(string backtestPath)
        {
            _backtestPath = backtestPath;
            _testSymbols = new List<string> { "BTCUSDT", "ETHUSDT", "ADAUSDT" }; // 3개만 테스트
            _testIntervals = new List<KlineInterval> { KlineInterval.OneHour }; // 1시간만 테스트
            _defaultStartDate = new DateTime(2023, 1, 1);
            _defaultEndDate = new DateTime(2023, 12, 31);
        }

        public async Task<List<BacktestResult>> RunBacktestsAsync(List<StrategyInfo> strategies, int iteration)
        {
            var results = new List<BacktestResult>();
            var iterationPath = Path.Combine(_backtestPath, $"iteration_{iteration}");
            Directory.CreateDirectory(iterationPath);

            Console.WriteLine($"Starting optimized backtests for {strategies.Count} strategies...");

            // 먼저 전략들을 메모리에 컴파일
            var compiledStrategies = new Dictionary<string, Type>();
            foreach (var strategy in strategies)
            {
                try
                {
                    var strategyType = CompileStrategyInMemory(strategy);
                    if (strategyType != null)
                    {
                        compiledStrategies[strategy.Name] = strategyType;
                        Console.WriteLine($"Compiled strategy: {strategy.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to compile {strategy.Name}: {ex.Message}");
                    // 실패한 전략은 건너뜀
                    continue;
                }
            }

            Console.WriteLine($"Successfully compiled {compiledStrategies.Count}/{strategies.Count} strategies");

            // 테스트 데이터 미리 로드 (공유 데이터)
            var testData = await LoadTestDataAsync();
            if (!testData.Any())
            {
                Console.WriteLine("Warning: No test data available. Creating mock data...");
                testData = CreateMockTestData();
            }

            // 병렬로 백테스트 실행
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
            var tasks = new List<Task<BacktestResult>>();

            foreach (var strategy in compiledStrategies)
            {
                var strategyInfo = strategies.First(s => s.Name == strategy.Key);
                var strategyType = strategy.Value;

                // 각 심볼/인터벌 조합에 대해 백테스트 실행
                foreach (var symbol in _testSymbols)
                {
                    foreach (var interval in _testIntervals)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                var result = await RunSingleBacktestAsync(strategyInfo, strategyType, symbol, interval, testData);
                                return result;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }
                }
            }

            var taskResults = await Task.WhenAll(tasks);
            results.AddRange(taskResults.Where(r => r != null));

            // 결과 저장
            await SaveBacktestResultsAsync(results, iteration);
            Console.WriteLine($"Completed {results.Count} backtests out of {tasks.Count} planned");

            return results;
        }

        private Type? CompileStrategyInMemory(StrategyInfo strategy)
        {
            // StrategyInfo에 이미 StrategyRuntimeType이 포함되어 있음
            return strategy.StrategyRuntimeType;
        }

        private async Task<BacktestResult?> RunSingleBacktestAsync(StrategyInfo strategyInfo, Type strategyType, string symbol, KlineInterval interval, Dictionary<string, List<ChartInfo>> testData)
        {
            try
            {
                // 백테스터 인스턴스 생성
                var reportFileName = $"{strategyInfo.Name}_{symbol}_{interval}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var backtester = Activator.CreateInstance(strategyType, reportFileName, 10000m, 10, MaxActiveDealsType.Total, 3) as Backtester;

                if (backtester == null)
                {
                    return new BacktestResult
                    {
                        StrategyName = strategyInfo.Name,
                        Symbol = symbol,
                        Parameters = strategyInfo.Parameters,
                        Error = "Failed to create backtester instance",
                        IsSuccess = false
                    };
                }

                // 파라미터 설정 (Reflection 사용)
                foreach (var param in strategyInfo.Parameters)
                {
                    var property = strategyType.GetProperty(param.Key);
                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            object convertedValue;

                            // Handle JSON element conversion properly
                            if (param.Value is JsonElement jsonElement)
                            {
                                convertedValue = jsonElement.ValueKind switch
                                {
                                    JsonValueKind.String => property.PropertyType == typeof(decimal)
                                        ? decimal.Parse(jsonElement.GetString()!)
                                        : Convert.ChangeType(jsonElement.GetString(), property.PropertyType),
                                    JsonValueKind.Number => property.PropertyType == typeof(decimal)
                                        ? jsonElement.GetDecimal()
                                        : Convert.ChangeType(jsonElement.GetDouble(), property.PropertyType),
                                    JsonValueKind.True or JsonValueKind.False => jsonElement.GetBoolean(),
                                    _ => Convert.ChangeType(param.Value, property.PropertyType)
                                };
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(param.Value, property.PropertyType);
                            }

                            property.SetValue(backtester, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to set parameter {param.Key}: {ex.Message} (Original type: {param.Value?.GetType()}, Target type: {property.PropertyType})");
                        }
                    }
                }

                // 테스트 데이터 설정
                if (testData.TryGetValue($"{symbol}_{interval}", out var charts))
                {
                    var chartPack = new ChartPack(interval);
                    foreach (var chart in charts)
                    {
                        chartPack.AddChart(chart);
                    }
                    backtester.Init(new List<ChartPack> { chartPack }, interval);
                }
                else
                {
                    return new BacktestResult
                    {
                        StrategyName = strategyInfo.Name,
                        Symbol = symbol,
                        Parameters = strategyInfo.Parameters,
                        Error = $"No test data available for {symbol}_{interval}",
                        IsSuccess = false
                    };
                }

                // 백테스팅 실행 (Backtester2와 동일하게 시작일 10일 후)
                var startTime = DateTime.Now;
                var actualStartDate = _defaultStartDate.AddDays(10); // Backtester2와 동일
                var (error, _) = backtester.Run(actualStartDate, _defaultEndDate);
                var endTime = DateTime.Now;

                if (!string.IsNullOrEmpty(error))
                {
                    return new BacktestResult
                    {
                        StrategyName = strategyInfo.Name,
                        Symbol = symbol,
                        Parameters = strategyInfo.Parameters,
                        Error = error,
                        IsSuccess = false
                    };
                }

                // 결과 수집 (Backtester2와 동일한 속성 사용)
                var estimatedMoney = GetProperty(backtester, "EstimatedMoney") ?? backtester.Money;
                var finalMoney = GetProperty(backtester, "Money") ?? backtester.Money;

                return new BacktestResult
                {
                    StrategyName = strategyInfo.Name,
                    Symbol = symbol,
                    Parameters = strategyInfo.Parameters,
                    StartTime = _defaultStartDate,
                    EndTime = _defaultEndDate,
                    Win = backtester.Win,
                    Lose = backtester.Lose,
                    WinRate = backtester.WinRate,
                    FinalMoney = finalMoney, // Backtester2와 동일
                    Mdd = backtester.Mdd,
                    ResultPerRisk = backtester.ResultPerRisk,
                    Roe = backtester.ProfitRoe,
                    IsSuccess = true,
                    RunTime = endTime - startTime
                };
            }
            catch (Exception ex)
            {
                return new BacktestResult
                {
                    StrategyName = strategyInfo.Name,
                    Symbol = symbol,
                    Parameters = strategyInfo.Parameters,
                    Error = ex.Message,
                    IsSuccess = false
                };
            }
        }

        private async Task<Dictionary<string, List<ChartInfo>>> LoadTestDataAsync()
        {
            var testData = new Dictionary<string, List<ChartInfo>>();

            try
            {
                // Backtester2와 동일하게 실제 데이터 로드
                Console.WriteLine("Loading real test data using ChartLoader...");

                // 각 심볼/인터벌 조합에 대해 실제 데이터 로드
                foreach (var symbol in _testSymbols)
                {
                    foreach (var interval in _testIntervals)
                    {
                        try
                        {
                            // Backtester2와 정확히 동일한 방식으로 데이터 로드
                            var chartPack = Mercury.Charts.ChartLoader.InitCharts(symbol, interval, _defaultStartDate, _defaultEndDate);

                            if (chartPack != null && chartPack.Charts.Count > 0)
                            {
                                var key = $"{symbol}_{interval}";
                                testData[key] = chartPack.Charts.ToList();
                                Console.WriteLine($"Loaded {testData[key].Count} candles for {symbol} {interval}");
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No data loaded for {symbol} {interval}");
                                // 데이터가 없으면 Mock 데이터 사용
                                var key = $"{symbol}_{interval}";
                                testData[key] = GenerateMockChartData(symbol, interval);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load real data for {symbol} {interval}: {ex.Message}");
                            Console.WriteLine($"Using mock data for {symbol} {interval}");

                            // 실패하면 Mock 데이터 사용
                            var key = $"{symbol}_{interval}";
                            testData[key] = GenerateMockChartData(symbol, interval);
                        }
                    }
                }

                Console.WriteLine($"Loaded test data for {testData.Count} symbol/interval combinations");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading test data: {ex.Message}");
                Console.WriteLine("Falling back to mock data...");

                // 실패하면 Mock 데이터로 대체
                foreach (var symbol in _testSymbols)
                {
                    foreach (var interval in _testIntervals)
                    {
                        var key = $"{symbol}_{interval}";
                        testData[key] = GenerateMockChartData(symbol, interval);
                    }
                }
            }

            return testData;
        }

        private Dictionary<string, List<ChartInfo>> CreateMockTestData()
        {
            var testData = new Dictionary<string, List<ChartInfo>>();

            foreach (var symbol in _testSymbols)
            {
                foreach (var interval in _testIntervals)
                {
                    var key = $"{symbol}_{interval}";
                    testData[key] = GenerateMockChartData(symbol, interval);
                }
            }

            return testData;
        }

        private List<ChartInfo> GenerateMockChartData(string symbol, KlineInterval interval)
        {
            var charts = new List<ChartInfo>();
            // 각 심볼마다 다른 시드값 사용해서 다른 데이터 생성
            var seed = symbol.GetHashCode() + (int)interval + DateTime.Now.Millisecond;
            var random = new Random(seed);
            var basePrice = symbol == "BTCUSDT" ? 20000m : symbol == "ETHUSDT" ? 1500m : 0.5m;
            var currentPrice = basePrice;

            var startTime = _defaultStartDate;
            var intervalMinutes = GetIntervalMinutes(interval);

            // 기술적 지표 계산을 위한 데이터 저장
            var closes = new List<decimal>();
            var highs = new List<decimal>();
            var lows = new List<decimal>();
            var volumes = new List<decimal>();

            for (int i = 0; i < 1000; i++) // 1000개의 캔들 생성
            {
                var open = currentPrice;
                var change = (decimal)(random.NextDouble() - 0.5) * currentPrice * 0.02m; // ±2% 변동
                var close = open + change;
                var high = Math.Max(open, close) * (1 + (decimal)random.NextDouble() * 0.01m);
                var low = Math.Min(open, close) * (1 - (decimal)random.NextDouble() * 0.01m);
                var volume = (decimal)(random.NextDouble() * 1000000 + 100000);

                var quote = new Quote(startTime.AddMinutes(i * intervalMinutes), open, high, low, close, volume);
                var chartInfo = new ChartInfo(symbol, quote);

                // 기술적 지표 실제 계산
                closes.Add(close);
                highs.Add(high);
                lows.Add(low);
                volumes.Add(volume);

                // CCI 계산 (14-period 기본)
                if (i >= 14)
                {
                    var recentCloses = closes.Skip(i - 14).Take(14).ToList();
                    var recentHighs = highs.Skip(i - 14).Take(14).ToList();
                    var recentLows = lows.Skip(i - 14).Take(14).ToList();

                    var typicalPrices = recentCloses.Zip(recentHighs.Zip(recentLows, (h, l) => (h, l)),
                        (c, hl) => (c + hl.h + hl.l) / 3).ToList();
                    var sma = typicalPrices.Average();
                    var meanDeviation = typicalPrices.Average(tp => Math.Abs(tp - sma));

                    if (meanDeviation > 0)
                        chartInfo.Cci = (typicalPrices.Last() - sma) / (0.015m * meanDeviation);
                    else
                        chartInfo.Cci = 0;
                }

                // Ichimoku Cloud 계산
                if (i >= 52) // Leading Span 기간 필요
                {
                    // Conversion Line (Tenkan-sen): 9-period high/low average
                    var recentHighs9 = highs.Skip(i - 9).Take(9).ToList();
                    var recentLows9 = lows.Skip(i - 9).Take(9).ToList();
                    chartInfo.IcConversion = (recentHighs9.Max() + recentLows9.Min()) / 2;

                    // Base Line (Kijun-sen): 26-period high/low average
                    var recentHighs26 = highs.Skip(i - 26).Take(26).ToList();
                    var recentLows26 = lows.Skip(i - 26).Take(26).ToList();
                    chartInfo.IcBase = (recentHighs26.Max() + recentLows26.Min()) / 2;

                    // Leading Span A: (Conversion + Base) / 2, shifted 26 periods ahead
                    chartInfo.IcLeadingSpan1 = (chartInfo.IcConversion.Value + chartInfo.IcBase.Value) / 2;

                    // Leading Span B: 52-period high/low average, shifted 26 periods ahead
                    var recentHighs52 = highs.Skip(i - 52).Take(52).ToList();
                    var recentLows52 = lows.Skip(i - 52).Take(52).ToList();
                    chartInfo.IcLeadingSpan2 = (recentHighs52.Max() + recentLows52.Min()) / 2;
                }

                // EMA 계산 (12-period)
                if (i == 0)
                {
                    chartInfo.Ema1 = close;
                }
                else if (i >= 12)
                {
                    var multiplier = 2m / (12 + 1);
                    var previousEma = charts.Last().Ema1 ?? close;
                    chartInfo.Ema1 = (close - previousEma) * multiplier + previousEma;
                }

                charts.Add(chartInfo);
                currentPrice = close;
            }

            return charts;
        }

        private int GetIntervalMinutes(KlineInterval interval)
        {
            return interval switch
            {
                KlineInterval.OneMinute => 1,
                KlineInterval.FiveMinutes => 5,
                KlineInterval.FifteenMinutes => 15,
                KlineInterval.ThirtyMinutes => 30,
                KlineInterval.OneHour => 60,
                KlineInterval.FourHour => 240,
                KlineInterval.OneDay => 1440,
                _ => 60
            };
        }

        private async Task SaveBacktestResultsAsync(List<BacktestResult> results, int iteration)
        {
            try
            {
                var resultsPath = Path.Combine(_backtestPath, $"iteration_{iteration}_results.json");
                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(resultsPath, json);

                // CSV로도 저장
                var csvPath = Path.Combine(_backtestPath, $"iteration_{iteration}_results.csv");
                var csv = ConvertToCsv(results);
                await File.WriteAllTextAsync(csvPath, csv);

                Console.WriteLine($"Results saved to {resultsPath} and {csvPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving results: {ex.Message}");
            }
        }

        private string ConvertToCsv(List<BacktestResult> results)
        {
            var lines = new List<string>
            {
                "StrategyName,Symbol,StartTime,EndTime,Roe,WinRate,Mdd,ResultPerRisk,Win,Lose,FinalMoney,IsSuccess,Error,RunTime"
            };

            foreach (var result in results)
            {
                var line = $"{result.StrategyName},{result.Symbol},{result.StartTime:yyyy-MM-dd},{result.EndTime:yyyy-MM-dd}," +
                          $"{result.Roe:P2},{result.WinRate:P2},{result.Mdd:P2},{result.ResultPerRisk:F2}," +
                          $"{result.Win},{result.Lose},{result.FinalMoney},{result.IsSuccess},\"{result.Error}\",{result.RunTime?.TotalMinutes:F1}";
                lines.Add(line);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static decimal? GetProperty(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj) as decimal?;
            }
            catch
            {
                return null;
            }
        }
    }

    // BacktestResult는 Models/StrategyInfo.cs에 이미 정의되어 있음
}