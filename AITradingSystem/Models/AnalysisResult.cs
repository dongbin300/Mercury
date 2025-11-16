using System;
using System.Collections.Generic;
using System.Text;

namespace AITradingSystem.Models
{
    public class AnalysisResult
    {
        public List<Weakness> Weaknesses { get; set; } = new List<Weakness>();
        public Dictionary<string, double> MarketConditionPerformance { get; set; } = new Dictionary<string, double>();
        public List<ImprovementSuggestion> ImprovementSuggestions { get; set; } = new List<ImprovementSuggestion>();
    }

    public class Weakness
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public double Impact { get; set; }
        public string Suggestion { get; set; }
    }

    public class ImprovementSuggestion
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> NewParameters { get; set; }
        public double ExpectedImprovement { get; set; }
    }
}
