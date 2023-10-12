using MercuryTradingModel.Assets;
using MercuryTradingModel.Charts;
using MercuryTradingModel.Enums;
using MercuryTradingModel.Trades;

namespace MercuryTradingModel.Interfaces
{
    public interface IOrder
    {
        OrderType Type { get; set; }
        PositionSide Side { get; set; }
        OrderAmount Amount { get; set; }
        decimal? Price { get; set; }
        decimal MakerFee { get; }
        decimal TakerFee { get; }

        BackTestTradeInfo Run(Asset asset, MercuryChartInfo chartm, string tag);
    }
}
