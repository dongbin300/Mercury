﻿using MercuryTradingModel.Assets;
using MercuryTradingModel.Charts;
using MercuryTradingModel.Enums;
using MercuryTradingModel.Interfaces;
using MercuryTradingModel.Trades;

namespace MercuryTradingModel.Orders
{
    public class Order : IOrder
    {
        public OrderType Type { get; set; } = OrderType.None;
        public PositionSide Side { get; set; } = PositionSide.None;
        public OrderAmount Amount { get; set; } = new OrderAmount(OrderAmountType.None, 0);
        public decimal? Price { get; set; } = null;
        public decimal MakerFee => 0.00075m; // use BNB(0.075%)
        public decimal TakerFee => 0.00075m; // use BNB(0.075%)

        public Order()
        {
        }

        public BackTestTradeInfo Run(Asset asset, MercuryChartInfo chart, string tag = "")
        {
            return new BackTestTradeInfo("", "", "", "", "", "", "", "", "", "", "");
        }

        public Order Position(PositionSide side, OrderAmount amount)
        {
            return this;
        }

        public Order Position(PositionSide side, OrderAmountType orderType, double amount)
        {
            return this;
        }

        public Order Close(OrderAmount amount)
        {
            return this;
        }

        public Order Close(OrderAmountType orderType, double amount)
        {
            return this;
        }
    }
}
