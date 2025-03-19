using Mercury.Enums;
using Mercury.Interfaces;

namespace Mercury.Elements
{
	public class TradeElement : IElement
	{
		public MtmTradeElementType ElementType { get; set; }
		public decimal Value { get; set; }

		public TradeElement(MtmTradeElementType elementType, decimal value)
		{
			ElementType = elementType;
			Value = value;
		}

		public override string ToString()
		{
			return ElementType.ToString() + " " + Value.ToString();
		}
	}
}
