using MercuryTradingModel.Enums;

namespace MercuryTradingModel.Elements
{
    public class TradeElementResult
    {
        public TradeElementType Type { get; set; }
        public decimal? Value { get; set; }

        public TradeElementResult(TradeElementType type, decimal? value)
        {
            Type = type;
            Value = value;
        }
    }
}
