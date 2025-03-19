using Mercury.Assets;
using Mercury.Charts;
using Mercury.Enums;
using Mercury.Interfaces;
using Mercury.Trades;

namespace Mercury.Orders
{
	public class Order : IOrder
	{
		public MtmOrderType Type { get; set; } = MtmOrderType.None;
		public MtmPositionSide Side { get; set; } = MtmPositionSide.None;
		public OrderAmount Amount { get; set; } = new OrderAmount(MtmOrderAmountType.None, 0);
		public decimal? Price { get; set; } = null;
		public decimal MakerFee => 0.00075m; // use BNB(0.075%)
		public decimal TakerFee => 0.00075m; // use BNB(0.075%)

		public Order()
		{
		}

		public BackTestTradeInfo Run(Asset asset, ChartInfo chart, string tag = "")
		{
			return new BackTestTradeInfo("", "", "", "", "", "", "", "", "", "", "");
		}

		public Order Position(MtmPositionSide side, OrderAmount amount)
		{
			return this;
		}

		public Order Position(MtmPositionSide side, MtmOrderAmountType orderType, double amount)
		{
			return this;
		}

		public Order Close(OrderAmount amount)
		{
			return this;
		}

		public Order Close(MtmOrderAmountType orderType, double amount)
		{
			return this;
		}
	}
}
