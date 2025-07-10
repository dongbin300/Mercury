using Mercury.Enums;

namespace Mercury.Elements
{
	public class TradeElementResult(MtmTradeElementType type, decimal? value)
	{
		public MtmTradeElementType Type { get; set; } = type;
		public decimal? Value { get; set; } = value;
	}
}
