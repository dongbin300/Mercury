using Mercury.Enums;

namespace Mercury.Elements
{
	public class TradeElementResult
	{
		public MtmTradeElementType Type { get; set; }
		public decimal? Value { get; set; }

		public TradeElementResult(MtmTradeElementType type, decimal? value)
		{
			Type = type;
			Value = value;
		}
	}
}
