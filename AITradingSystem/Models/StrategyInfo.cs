namespace Mercury.AITradingSystem.Models
{
    public class StrategyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string StrategyType { get; set; } = string.Empty; // "CCI", "EMA", "MACD", etc.
        public string Description { get; set; } = string.Empty;
        public int Generation { get; set; } = 0;
        public List<string> ParentStrategies { get; set; } = new();

        // 런타임에 사용될 실제 타입
        public Type? StrategyRuntimeType { get; set; }

        // Performance metrics
        public decimal AverageRoe { get; set; }
        public decimal AverageWinRate { get; set; }
        public decimal AverageMdd { get; set; }
        public decimal AverageResultPerRisk { get; set; }
        public int TotalTrades { get; set; }
        public List<BacktestResult> IndividualResults { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class BacktestResult
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal Roe { get; set; }
        public decimal WinRate { get; set; }
        public decimal Mdd { get; set; }
        public decimal ResultPerRisk { get; set; }
        public int Win { get; set; }
        public int Lose { get; set; }
        public decimal FinalMoney { get; set; }
        public string Error { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = true;
        public TimeSpan? RunTime { get; set; }
    }

    public class AnalysisResult
    {
        public List<StrategyInfo> TopStrategies { get; set; } = new();
        public List<BacktestResult> AllResults { get; set; } = new();
        public int TotalStrategies { get; set; }
        public int ViableStrategies { get; set; } // ROI > 0 and WinRate > 40%
        public decimal AverageRoe { get; set; }
        public decimal AverageWinRate { get; set; }
        public decimal AverageMdd { get; set; }
        public Dictionary<string, decimal> ParameterImportance { get; set; } = new();
        public List<string> RecommendedImprovements { get; set; } = new();
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }

    public class StrategyTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "CCI", "EMA", "MACD", "Hybrid"
        public string TemplateCode { get; set; } = string.Empty;
        public List<ParameterDefinition> Parameters { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredIndicators { get; set; } = new();
        public ComplexityLevel Complexity { get; set; } = ComplexityLevel.Medium;
    }

    public class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public Type Type { get; set; } = typeof(int);
        public object DefaultValue { get; set; } = default!;
        public object MinValue { get; set; } = default!;
        public object MaxValue { get; set; } = default!;
        public bool IsVariable { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public decimal MutationRate { get; set; } = 0.1m; // for genetic algorithms
    }

    public enum ComplexityLevel
    {
        Simple,
        Medium,
        Complex,
        Advanced
    }
}