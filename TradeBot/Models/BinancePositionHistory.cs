using Binance.Net.Enums;

using System;
using System.ComponentModel;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinancePositionHistory : BinanceHistory
	{
		public DateTime EntryTime { get; set; }
		public DateTime? ExitTime { get; set; }
		public string Symbol { get; set; }
		public PositionSide PositionSide { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? ExitPrice { get; set; }
		public decimal EntryQuantity { get; set; }
		public decimal ExitQuantity { get; set; }
		public decimal RealizedPnl { get; set; }
		public bool IsClosed => Math.Abs(EntryQuantity - ExitQuantity) < 0.000001m;

		[Browsable(false)]
		public SolidColorBrush PositionSideColor => PositionSide == PositionSide.Long ? Common.LongColor : Common.ShortColor;
		[Browsable(false)]
		public SolidColorBrush RealizedPnlColor => RealizedPnl > 0 ? Common.LongColor : RealizedPnl < 0 ? Common.ShortColor : Common.ForegroundColor;

		public BinancePositionHistory(DateTime entryTime, DateTime? exitTime, string symbol, PositionSide positionSide, decimal entryPrice, decimal? exitPrice, decimal entryQuantity)
		{
			EntryTime = entryTime;
			ExitTime = exitTime;
			Symbol = symbol;
			PositionSide = positionSide;
			EntryPrice = entryPrice;
			ExitPrice = exitPrice;
			EntryQuantity = entryQuantity;
			ExitQuantity = 0m;
			RealizedPnl = 0m;
		}

		public override string ToString()
		{
			return $"{EntryTime:yyyy-MM-dd HH:mm:ss},{ExitTime:yyyy-MM-dd HH:mm:ss},{Symbol},{PositionSide},{EntryPrice},{ExitPrice},{EntryQuantity},{ExitQuantity},{RealizedPnl},{(IsClosed ? "Closed" : "Opened")}";
		}
	}
}
