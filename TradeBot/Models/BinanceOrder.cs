using Binance.Net.Enums;

using System;

namespace TradeBot.Models
{
    public class BinanceOrder
    {
        public long Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public PositionSide Side { get; set; }
        public DateTime CreateTime { get; set; }
        public FuturesOrderType Type { get; set; }
        public decimal Quantity { get; set; }

        public BinanceOrder(long id, string symbol, PositionSide side, FuturesOrderType type, decimal quantity, DateTime createTime)
        {
            Id = id;
            Symbol = symbol;
            Side = side;
            Type = type;
            Quantity = quantity;
            CreateTime = createTime;
        }
    }
}
