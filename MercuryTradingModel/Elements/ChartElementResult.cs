using MercuryTradingModel.Enums;

namespace MercuryTradingModel.Elements
{
    public class ChartElementResult
    {
        public ChartElementType Type { get; set; }
        public decimal? Value { get; set; }

        public ChartElementResult(ChartElementType type, decimal? value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Type}, {Value}";
        }
    }
}
