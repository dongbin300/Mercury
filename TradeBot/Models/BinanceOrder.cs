using Binance.Net.Enums;

using System;

namespace TradeBot.Models
{
	public class BinanceOrder(long id, string symbol, PositionSide side, FuturesOrderType type, decimal quantity, DateTime createTime)
	{
		public long Id { get; set; } = id;
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;
		public DateTime CreateTime { get; set; } = createTime;
		public FuturesOrderType Type { get; set; } = type;
		public decimal Quantity { get; set; } = quantity;
	}
}
