using Binance.Net.Enums;
using Mercury.AITradingSystem.Models;
using Mercury.Backtests;
using System.Text.Json;
using System.Reflection;

namespace Mercury.AITradingSystem
{
    public class StrategyGenerator
    {
        private readonly string _strategiesPath;
        private readonly List<StrategyTemplate> _templates;
        private readonly Random _random = new Random();
        private readonly Dictionary<string, Type> _availableStrategies;

        public StrategyGenerator(string strategiesPath)
        {
            _strategiesPath = strategiesPath;
            _templates = InitializeTemplates();
            _availableStrategies = LoadAvailableStrategies();
        }

        public async Task<List<StrategyInfo>> GenerateInitialStrategySetAsync()
        {
            var strategies = new List<StrategyInfo>();

            Console.WriteLine("Loading available strategy types from Mercury...");

            // Mercury에서 사용 가능한 전략 타입들을 가져옴
            var strategyTypes = _availableStrategies.Keys.ToList();
            if (!strategyTypes.Any())
            {
                Console.WriteLine("No strategies found. Using fallback strategies...");
                strategyTypes = new List<string> { "CCI", "EMA", "MACD", "RSI" };
            }

            // 각 타입별로 여러 파라미터 조합 생성
            foreach (var type in strategyTypes.Take(5)) // 상위 5개 타입만 사용
            {
                for (int i = 0; i < 4; i++) // 각 타입별 4개 전략
                {
                    var strategy = CreateStrategyInstance(type, 0);
                    if (strategy != null)
                    {
                        strategies.Add(strategy);
                    }
                }
            }

            // 생성된 전략들을 파일에 저장
            await SaveStrategiesAsync(strategies);

            return strategies;
        }

        public async Task<List<StrategyInfo>> GenerateImprovedStrategiesAsync(List<StrategyInfo> parentStrategies, Dictionary<string, decimal> parameterImportance)
        {
            var improvedStrategies = new List<StrategyInfo>();

            foreach (var parent in parentStrategies)
            {
                // 각 부모 전략에서 3개의 개선된 전략 생성
                for (int i = 0; i < 3; i++)
                {
                    var improvedStrategy = MutateStrategy(parent, parameterImportance);
                    improvedStrategies.Add(improvedStrategy);
                }
            }

            // 새로운 조합 전략 생성 (상위 2개 전략 결합)
            if (parentStrategies.Count >= 2)
            {
                var hybridStrategy = CreateHybridStrategy(parentStrategies.Take(2).ToList());
                if (hybridStrategy != null)
                {
                    improvedStrategies.Add(hybridStrategy);
                }
            }

            await SaveStrategiesAsync(improvedStrategies);
            return improvedStrategies;
        }

        private StrategyInfo CreateStrategyInstance(string type, int generation)
        {
            try
            {
                if (!_availableStrategies.ContainsKey(type))
                {
                    Console.WriteLine($"Strategy type {type} not found, using fallback");
                    type = "CCI"; // 기본값으로 fallback
                }

                var strategyType = _availableStrategies[type];
                var strategyName = $"{type}Strategy{_random.Next(1000, 9999)}";
                var className = $"AI{type}{strategyName.Split("Strategy")[1]}";

                // 파라미터 생성
                var parameters = GenerateRandomParametersForType(type);

                // StrategyInfo 생성
                var strategy = new StrategyInfo
                {
                    Name = strategyName,
                    ClassName = className,
                    Code = "", // 코드는 필요 없음 (기존 타입 사용)
                    Parameters = parameters,
                    StrategyType = type,
                    Description = $"AI generated {type} strategy variant",
                    Generation = generation,
                    CreatedAt = DateTime.UtcNow,
                    StrategyRuntimeType = strategyType
                };

                return strategy;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating strategy instance for type {type}: {ex.Message}");
                return null;
            }
        }

        private Dictionary<string, object> GenerateRandomParametersForType(string type)
        {
            var parameters = new Dictionary<string, object>();

            switch (type)
            {
                case "CCI":
                    parameters["EntryCci"] = _random.Next(-200, -100);
                    parameters["ExitCci"] = _random.Next(150, 250);
                    parameters["CciPeriod"] = _random.Next(14, 30);
                    break;

                case "EMA":
                    parameters["EmaPeriod"] = _random.Next(20, 100);
                    parameters["EntryThreshold"] = _random.Next(1, 5) * 0.001m;
                    parameters["ExitThreshold"] = _random.Next(1, 5) * 0.001m;
                    break;

                case "MACD":
                    parameters["MacdFast"] = _random.Next(10, 15);
                    parameters["MacdSlow"] = _random.Next(20, 30);
                    parameters["MacdSignal"] = _random.Next(8, 12);
                    break;

                case "RSI":
                    parameters["RsiPeriod"] = _random.Next(10, 20);
                    parameters["Oversold"] = _random.Next(20, 30);
                    parameters["Overbought"] = _random.Next(70, 80);
                    break;

                default:
                    // 기본 파라미터
                    parameters["Period"] = _random.Next(10, 50);
                    parameters["Threshold"] = _random.Next(-100, 100);
                    break;
            }

            return parameters;
        }

        private Dictionary<string, Type> LoadAvailableStrategies()
        {
            var strategies = new Dictionary<string, Type>();

            try
            {
                // Mercury 어셈블리에서 Backtester를 상속받는 모든 타입을 찾음
                var mercuryAssembly = typeof(Mercury.Backtests.Backtester).Assembly;
                var backtesterTypes = mercuryAssembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(Mercury.Backtests.Backtester)) && !t.IsAbstract)
                    .ToList();

                foreach (var type in backtesterTypes)
                {
                    var typeName = type.Name;
                    var strategyType = ExtractStrategyType(typeName);

                    if (!string.IsNullOrEmpty(strategyType))
                    {
                        strategies[strategyType] = type;
                        Console.WriteLine($"Found strategy: {strategyType} -> {type.Name}");
                    }
                }

                // 기본 전략들 추가 (fallback)
                if (!strategies.ContainsKey("CCI"))
                {
                    strategies["CCI"] = typeof(Mercury.Backtests.BacktestStrategies.Cci1);
                }

                Console.WriteLine($"Loaded {strategies.Count} strategy types");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading strategies: {ex.Message}");
            }

            return strategies;
        }

        private string ExtractStrategyType(string className)
        {
            // 클래스 이름에서 전략 타입 추출
            if (className.StartsWith("Cci") || className.Contains("CCI"))
                return "CCI";
            if (className.StartsWith("Ema") || className.Contains("EMA"))
                return "EMA";
            if (className.StartsWith("Macd") || className.Contains("MACD"))
                return "MACD";
            if (className.StartsWith("Rsi") || className.Contains("RSI"))
                return "RSI";
            if (className.Contains("Hybrid"))
                return "Hybrid";
            if (className.Contains("Candle"))
                return "Candle";

            return className; // 그대로 반환
        }

        private StrategyInfo MutateStrategy(StrategyInfo parent, Dictionary<string, decimal> parameterImportance)
        {
            var mutatedParams = new Dictionary<string, object>();

            foreach (var param in parent.Parameters)
            {
                if (!parameterImportance.ContainsKey(param.Key) || _random.NextDouble() > 0.7) // 30% 확률로 변이
                {
                    mutatedParams[param.Key] = param.Value;
                    continue;
                }

                var importance = parameterImportance[param.Key];
                var mutationRate = 0.1m + importance * 0.2m; // 중요도에 따라 변이율 조절

                var mutatedValue = MutateParameterValue(param.Value, mutationRate);
                mutatedParams[param.Key] = mutatedValue;
            }

            var strategyName = $"{parent.StrategyType}Mutated{_random.Next(1000, 9999)}";
            var className = $"AI{parent.StrategyType}{strategyName.Split("Mutated")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = mutatedParams,
                StrategyType = parent.StrategyType,
                Description = $"Mutated from {parent.Name}",
                Generation = parent.Generation + 1,
                ParentStrategies = new List<string> { parent.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = parent.StrategyRuntimeType
            };
        }

        private object MutateParameterValue(object currentValue, decimal mutationRate)
        {
            if (currentValue is int intVal)
            {
                var range = Math.Abs(intVal * 0.2m);
                var mutation = (int)(range * mutationRate);
                var newValue = intVal + _random.Next(-mutation, mutation + 1);
                return Math.Max(1, newValue); // 최소값 1 보장
            }
            else if (currentValue is decimal decimalVal)
            {
                var range = Math.Abs(decimalVal * 0.2m);
                var mutation = range * mutationRate;
                var delta = (decimal)(_random.NextDouble() * 2 - 1) * mutation;
                var newValue = decimalVal + delta;
                return Math.Max(0.01m, newValue);
            }
            else if (currentValue is bool boolVal)
            {
                return _random.NextDouble() < 0.1 ? !boolVal : boolVal;
            }

            return currentValue;
        }

        private StrategyInfo CreateHybridStrategy(List<StrategyInfo> parents)
        {
            // 하이브리드 전략 - 두 부모 전략의 파라미터를 결합
            var combinedParams = new Dictionary<string, object>();

            foreach (var parent in parents)
            {
                foreach (var param in parent.Parameters)
                {
                    if (!combinedParams.ContainsKey(param.Key))
                    {
                        combinedParams[param.Key] = param.Value;
                    }
                    else
                    {
                        // 두 값의 평균 사용
                        if (param.Value is int && combinedParams[param.Key] is int)
                        {
                            combinedParams[param.Key] = ((int)param.Value + (int)combinedParams[param.Key]) / 2;
                        }
                        else if (param.Value is decimal && combinedParams[param.Key] is decimal)
                        {
                            combinedParams[param.Key] = ((decimal)param.Value + (decimal)combinedParams[param.Key]) / 2;
                        }
                    }
                }
            }

            var strategyName = $"Hybrid{_random.Next(1000, 9999)}";
            var className = $"AIHybrid{strategyName.Split("Hybrid")[1]}";

            // 첫 번째 부모의 타입을 사용
            var strategyType = parents.First().StrategyRuntimeType;

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = combinedParams,
                StrategyType = "Hybrid",
                Description = $"Hybrid of {string.Join(" and ", parents.Select(p => p.Name))}",
                Generation = parents.Max(p => p.Generation) + 1,
                ParentStrategies = parents.Select(p => p.Name).ToList(),
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = strategyType
            };
        }

        private async Task SaveStrategiesAsync(List<StrategyInfo> strategies)
        {
            try
            {
                // 메타데이터만 저장 (실제 코드는 필요 없음)
                var metadataPath = Path.Combine(_strategiesPath, "strategies_metadata.json");
                var existingStrategies = new List<StrategyInfo>();

                if (File.Exists(metadataPath))
                {
                    var existingJson = await File.ReadAllTextAsync(metadataPath);
                    existingStrategies = JsonSerializer.Deserialize<List<StrategyInfo>>(existingJson) ?? new List<StrategyInfo>();
                }

                existingStrategies.AddRange(strategies);

                // StrategyRuntimeType은 직렬화에서 제외
                var serializableStrategies = existingStrategies.Select(s => new StrategyInfo
                {
                    Name = s.Name,
                    ClassName = s.ClassName,
                    Code = s.Code,
                    Parameters = s.Parameters,
                    StrategyType = s.StrategyType,
                    Description = s.Description,
                    Generation = s.Generation,
                    ParentStrategies = s.ParentStrategies,
                    StrategyRuntimeType = null, // 직렬화에서 제외
                    AverageRoe = s.AverageRoe,
                    AverageWinRate = s.AverageWinRate,
                    AverageMdd = s.AverageMdd,
                    AverageResultPerRisk = s.AverageResultPerRisk,
                    TotalTrades = s.TotalTrades,
                    IndividualResults = s.IndividualResults,
                    CreatedAt = s.CreatedAt,
                    IsActive = s.IsActive
                }).ToList();

                var json = JsonSerializer.Serialize(serializableStrategies, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metadataPath, json);

                Console.WriteLine($"Saved {strategies.Count} strategies to metadata");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving strategies: {ex.Message}");
            }
        }

        private List<StrategyTemplate> InitializeTemplates()
        {
            return new List<StrategyTemplate>
            {
                new StrategyTemplate
                {
                    Name = "CCI Template",
                    Type = "CCI",
                    Description = "CCI based strategy template",
                    Parameters = new List<ParameterDefinition>
                    {
                        new() { Name = "EntryCci", Type = typeof(int), DefaultValue = -150, MinValue = -300, MaxValue = -50 },
                        new() { Name = "ExitCci", Type = typeof(int), DefaultValue = 200, MinValue = 100, MaxValue = 300 },
                        new() { Name = "CciPeriod", Type = typeof(int), DefaultValue = 20, MinValue = 10, MaxValue = 50 }
                    }
                },
                // 다른 템플릿들도 추가 가능...
            };
        }
    }
}