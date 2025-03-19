using Mercury.Assets;
using Mercury.Charts;
using Mercury.Enums;
using Mercury.Trades;

namespace Mercury.Interfaces
{
	public interface IOrder
	{
		MtmOrderType Type { get; set; }
		MtmPositionSide Side { get; set; }
		OrderAmount Amount { get; set; }
		decimal? Price { get; set; }
		decimal MakerFee { get; }
		decimal TakerFee { get; }

		BackTestTradeInfo Run(Asset asset, ChartInfo chartm, string tag);
	}
}
