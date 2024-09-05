using Binance.Net.Enums;

using System;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinanceOrder(long id, string symbol, PositionSide side, FuturesOrderType type, decimal quantity, DateTime createTime, decimal price, decimal quantityFilled)
	{
		public long Id { get; set; } = id;
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;
		public SolidColorBrush PositionSideColor => Side == PositionSide.Long ? Common.LongColor : Common.ShortColor;
		public DateTime CreateTime { get; set; } = createTime;
		public FuturesOrderType Type { get; set; } = type;
		public decimal Quantity { get; set; } = quantity;
		public decimal Price { get; set; } = price;
		public decimal QuantityFilled { get; set; } = quantityFilled;
		public string FilledString => $"{QuantityFilled} / {Quantity}";
	}
}
