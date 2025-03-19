using Mercury.Enums;

namespace Mercury.Assets
{
	public class OrderAmount(MtmOrderAmountType orderType, decimal value)
	{
		public MtmOrderAmountType OrderType { get; set; } = orderType;
		public decimal Value { get; set; } = value;
	}
}
