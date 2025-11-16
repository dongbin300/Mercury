using Mercury.AITradingSystem.Models;
using System.Text.Json;

namespace Mercury.AITradingSystem
{
    public class StrategyImprover
    {
        private readonly string _improvementsPath;
        private readonly Random _random = new Random();

        public StrategyImprover(string improvementsPath)
        {
            _improvementsPath = improvementsPath;
            Directory.CreateDirectory(_improvementsPath);
        }

        public async Task<List<StrategyInfo>> ImproveStrategiesAsync(List<StrategyInfo> topStrategies, AnalysisResult analysis)
        {
            var improvedStrategies = new List<StrategyInfo>();

            Console.WriteLine($"Improving {topStrategies.Count} top strategies...");

            foreach (var strategy in topStrategies)
            {
                // 1. 파라미터 최적화 기반 개선
                var optimizedStrategy = OptimizeParameters(strategy, analysis);
                improvedStrategies.Add(optimizedStrategy);

                // 2. 로직 기반 개선 (승률/손실률 분석)
                if (strategy.AverageWinRate < 0.6m)
                {
                    var logicImprovedStrategy = ImproveEntryExitLogic(strategy);
                    improvedStrategies.Add(logicImprovedStrategy);
                }

                // 3. 리스크 관리 개선 (MDD가 높은 경우)
                if (strategy.AverageMdd > 0.15m)
                {
                    var riskImprovedStrategy = ImproveRiskManagement(strategy);
                    improvedStrategies.Add(riskImprovedStrategy);
                }
            }

            // 4. 하이브리드 전략 생성 (상위 2개 전략 결합)
            if (topStrategies.Count >= 2)
            {
                var hybridStrategy = CreateAdvancedHybrid(topStrategies.Take(2).ToList());
                improvedStrategies.Add(hybridStrategy);
            }

            // 5. 완전히 새로운 변형 생성
            var novelVariants = GenerateNovelVariants(topStrategies.First());
            improvedStrategies.AddRange(novelVariants);

            await SaveImprovementHistoryAsync(topStrategies, improvedStrategies, analysis);

            Console.WriteLine($"Generated {improvedStrategies.Count} improved strategies.");
            return improvedStrategies;
        }

        private StrategyInfo OptimizeParameters(StrategyInfo original, AnalysisResult analysis)
        {
            var optimizedParams = new Dictionary<string, object>();

            foreach (var param in original.Parameters)
            {
                var currentValue = param.Value;
                var importance = analysis.ParameterImportance.GetValueOrDefault(param.Key, 0.5m);

                // 중요한 파라미터는 더 세밀하게 조정
                var adjustmentRange = importance * 0.2m; // 중요도에 따라 조정 범위 결정

                object optimizedValue;

                if (currentValue is int intVal)
                {
                    var adjustment = (int)Math.Max(1, intVal * adjustmentRange);
                    var minRange = Math.Max(1, intVal - adjustment);
                    var maxRange = intVal + adjustment + 1;
                    var newValue = intVal + _random.Next(-adjustment, adjustment + 1);
                    optimizedValue = Math.Max(1, newValue); // 최소값 1 보장
                }
                else if (currentValue is decimal decimalVal)
                {
                    var adjustment = decimalVal * adjustmentRange;
                    var delta = (decimal)(_random.NextDouble() * 2 - 1) * adjustment;
                    optimizedValue = Math.Max(0.01m, decimalVal + delta);
                }
                else if (currentValue is bool boolVal)
                {
                    // bool 값은 낮은 확률로만 변경
                    optimizedValue = _random.NextDouble() < 0.1 ? !boolVal : boolVal;
                }
                else
                {
                    optimizedValue = currentValue;
                }

                optimizedParams[param.Key] = optimizedValue;
            }

            var strategyName = $"{original.StrategyType}Optimized{_random.Next(1000, 9999)}";
            var className = $"AI{original.StrategyType}{strategyName.Split("Optimized")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = optimizedParams,
                StrategyType = original.StrategyType,
                Description = $"Parameter-optimized version of {original.Name}",
                Generation = original.Generation + 1,
                ParentStrategies = new List<string> { original.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = original.StrategyRuntimeType
            };
        }

        private StrategyInfo ImproveEntryExitLogic(StrategyInfo original)
        {
            var improvedParams = new Dictionary<string, object>(original.Parameters);

            // 진입 조건 강화 (더 엄격한 조건)
            if (improvedParams.ContainsKey("EntryCci"))
            {
                var entryCci = (int)improvedParams["EntryCci"];
                improvedParams["EntryCci"] = entryCci - 10; // 더 낮은 CCI 값에서만 진입
            }

            // 청산 조건 개선
            if (improvedParams.ContainsKey("ExitCci"))
            {
                var exitCci = (int)improvedParams["ExitCci"];
                improvedParams["ExitCci"] = exitCci + 10; // 더 높은 CCI 값에서 청산
            }

            // 추가 확인 지표 파라미터
            if (!improvedParams.ContainsKey("VolumeMultiplier"))
            {
                improvedParams["VolumeMultiplier"] = 1.2m;
            }

            if (!improvedParams.ContainsKey("TrendConfirmation"))
            {
                improvedParams["TrendConfirmation"] = true;
            }

            var strategyName = $"{original.StrategyType}LogicImproved{_random.Next(1000, 9999)}";
            var className = $"AI{original.StrategyType}{strategyName.Split("LogicImproved")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = improvedParams,
                StrategyType = original.StrategyType,
                Description = $"Logic-improved version of {original.Name} with stricter entry/exit conditions",
                Generation = original.Generation + 1,
                ParentStrategies = new List<string> { original.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = original.StrategyRuntimeType
            };
        }

        private StrategyInfo ImproveRiskManagement(StrategyInfo original)
        {
            var improvedParams = new Dictionary<string, object>(original.Parameters);

            // 손절/익절 비율 조정
            if (!improvedParams.ContainsKey("StopLossAtrMultiplier"))
            {
                improvedParams["StopLossAtrMultiplier"] = 2.0m; // 더 타이트한 손절
            }

            if (!improvedParams.ContainsKey("TakeProfitAtrMultiplier"))
            {
                improvedParams["TakeProfitAtrMultiplier"] = 3.0m; // 더 보수적인 익절
            }

            if (!improvedParams.ContainsKey("MaxPositionSize"))
            {
                improvedParams["MaxPositionSize"] = 0.8m; // 포지션 크기 제한
            }

            // 포지션 분할 청산 파라미터
            if (!improvedParams.ContainsKey("UsePartialExit"))
            {
                improvedParams["UsePartialExit"] = true;
            }

            if (!improvedParams.ContainsKey("PartialExitPercent"))
            {
                improvedParams["PartialExitPercent"] = 0.5m;
            }

            var strategyName = $"{original.StrategyType}RiskImproved{_random.Next(1000, 9999)}";
            var className = $"AI{original.StrategyType}{strategyName.Split("RiskImproved")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = improvedParams,
                StrategyType = original.StrategyType,
                Description = $"Risk management improved version of {original.Name}",
                Generation = original.Generation + 1,
                ParentStrategies = new List<string> { original.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = original.StrategyRuntimeType
            };
        }

        private StrategyInfo CreateAdvancedHybrid(List<StrategyInfo> parents)
        {
            var hybridParams = new Dictionary<string, object>();

            // 부모 전략들의 파라미터를 지능적으로 결합
            foreach (var parent in parents)
            {
                foreach (var param in parent.Parameters)
                {
                    if (!hybridParams.ContainsKey(param.Key))
                    {
                        // 성능이 좋은 부모의 파라미터를 우선 선택
                        hybridParams[param.Key] = param.Value;
                    }
                    else
                    {
                        // 두 값의 평균 또는 더 보수적인 값 선택
                        if (param.Value is int && hybridParams[param.Key] is int)
                        {
                            hybridParams[param.Key] = ((int)param.Value + (int)hybridParams[param.Key]) / 2;
                        }
                        else if (param.Value is decimal && hybridParams[param.Key] is decimal)
                        {
                            hybridParams[param.Key] = ((decimal)param.Value + (decimal)hybridParams[param.Key]) / 2;
                        }
                    }
                }
            }

            // 하이브리드 전략 특유의 파라미터 추가
            hybridParams["UseMultipleTimeframe"] = true;
            hybridParams["SignalConfirmationCount"] = 2;
            hybridParams["RiskPerTrade"] = 0.02m; // 거래당 2% 리스크

            var strategyName = $"AdvancedHybrid{_random.Next(1000, 9999)}";
            var className = $"AIAdvancedHybrid{strategyName.Split("AdvancedHybrid")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = hybridParams,
                StrategyType = "AdvancedHybrid",
                Description = $"Advanced hybrid strategy combining {string.Join(" and ", parents.Select(p => p.Name))}",
                Generation = parents.Max(p => p.Generation) + 1,
                ParentStrategies = parents.Select(p => p.Name).ToList(),
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = parents.First().StrategyRuntimeType
            };
        }

        private List<StrategyInfo> GenerateNovelVariants(StrategyInfo baseStrategy)
        {
            var variants = new List<StrategyInfo>();

            // 변형 1: 반대 전략 (Long/Short 로직 교환)
            if (baseStrategy.StrategyType != "Hybrid")
            {
                var inverseStrategy = CreateInverseStrategy(baseStrategy);
                variants.Add(inverseStrategy);
            }

            // 변형 2: 보수적 버전
            var conservativeStrategy = CreateConservativeVariant(baseStrategy);
            variants.Add(conservativeStrategy);

            // 변형 3: 공격적 버전
            var aggressiveStrategy = CreateAggressiveVariant(baseStrategy);
            variants.Add(aggressiveStrategy);

            return variants;
        }

        private StrategyInfo CreateInverseStrategy(StrategyInfo original)
        {
            var inverseParams = new Dictionary<string, object>(original.Parameters);

            // CCI 진입/청산 값 반전
            if (inverseParams.ContainsKey("EntryCci") && inverseParams.ContainsKey("ExitCci"))
            {
                var temp = inverseParams["EntryCci"];
                inverseParams["EntryCci"] = inverseParams["ExitCci"];
                inverseParams["ExitCci"] = temp;
            }

            var strategyName = $"{original.StrategyType}Inverse{_random.Next(1000, 9999)}";
            var className = $"AI{original.StrategyType}{strategyName.Split("Inverse")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = inverseParams,
                StrategyType = original.StrategyType,
                Description = $"Inverse strategy of {original.Name} (swapped long/short logic)",
                Generation = original.Generation + 1,
                ParentStrategies = new List<string> { original.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = original.StrategyRuntimeType
            };
        }

        private StrategyInfo CreateConservativeVariant(StrategyInfo original)
        {
            var conservativeParams = new Dictionary<string, object>();

            foreach (var param in original.Parameters)
            {
                if (param.Value is int intVal)
                {
                    conservativeParams[param.Key] = (int)(intVal * 0.8); // 20% 보수적
                }
                else if (param.Value is decimal decimalVal)
                {
                    conservativeParams[param.Key] = decimalVal * 0.8m;
                }
                else
                {
                    conservativeParams[param.Key] = param.Value;
                }
            }

            // 추가 보수적 파라미터
            conservativeParams["ConfirmationCount"] = 2;
            conservativeParams["RiskReduction"] = 0.7m;

            var strategyName = $"{original.StrategyType}Conservative{_random.Next(1000, 9999)}";
            var className = $"AI{original.StrategyType}{strategyName.Split("Conservative")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = conservativeParams,
                StrategyType = original.StrategyType,
                Description = $"Conservative variant of {original.Name}",
                Generation = original.Generation + 1,
                ParentStrategies = new List<string> { original.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = original.StrategyRuntimeType
            };
        }

        private StrategyInfo CreateAggressiveVariant(StrategyInfo original)
        {
            var aggressiveParams = new Dictionary<string, object>();

            foreach (var param in original.Parameters)
            {
                if (param.Value is int intVal)
                {
                    aggressiveParams[param.Key] = (int)(intVal * 1.3); // 30% 공격적
                }
                else if (param.Value is decimal decimalVal)
                {
                    aggressiveParams[param.Key] = decimalVal * 1.3m;
                }
                else
                {
                    aggressiveParams[param.Key] = param.Value;
                }
            }

            // 추가 공격적 파라미터
            aggressiveParams["LeverageMultiplier"] = 1.5m;
            aggressiveParams["QuickEntry"] = true;

            var strategyName = $"{original.StrategyType}Aggressive{_random.Next(1000, 9999)}";
            var className = $"AI{original.StrategyType}{strategyName.Split("Aggressive")[1]}";

            return new StrategyInfo
            {
                Name = strategyName,
                ClassName = className,
                Parameters = aggressiveParams,
                StrategyType = original.StrategyType,
                Description = $"Aggressive variant of {original.Name}",
                Generation = original.Generation + 1,
                ParentStrategies = new List<string> { original.Name },
                CreatedAt = DateTime.UtcNow,
                StrategyRuntimeType = original.StrategyRuntimeType
            };
        }

        private async Task SaveImprovementHistoryAsync(List<StrategyInfo> originalStrategies, List<StrategyInfo> improvedStrategies, AnalysisResult analysis)
        {
            var history = new
            {
                Timestamp = DateTime.UtcNow,
                OriginalStrategies = originalStrategies.Select(s => new
                {
                    s.Name,
                    s.AverageRoe,
                    s.AverageWinRate,
                    s.AverageMdd
                }),
                ImprovedStrategies = improvedStrategies.Select(s => new
                {
                    s.Name,
                    s.StrategyType,
                    s.Description,
                    s.ParentStrategies,
                    s.Parameters
                }),
                AnalysisSummary = new
                {
                    analysis.TotalStrategies,
                    analysis.ViableStrategies,
                    analysis.AverageRoe,
                    analysis.AverageWinRate,
                    analysis.AverageMdd,
                    analysis.RecommendedImprovements
                }
            };

            var historyPath = Path.Combine(_improvementsPath, $"improvement_history_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(historyPath, json);
        }
    }
}