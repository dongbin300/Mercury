using Binance.Net.Enums;

using System;
using System.ComponentModel;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinanceTradeHistory : BinanceHistory
	{
		public DateTime Time { get; set; }
		public string Symbol { get; set; }
		public PositionSide PositionSide { get; set; }
		public OrderSide Side { get; set; }
		public decimal Price { get; set; }
		public decimal Quantity { get; set; }
		/// <summary>
		/// Price * Quantity
		/// </summary>
		public decimal QuoteQuantity { get; set; }
		public decimal Fee { get; set; }
		public string FeeAsset { get; set; }
		public decimal RealizedPnl { get; set; }
		public bool IsMaker { get; set; }

		[Browsable(false)]
		public SolidColorBrush PositionSideColor => PositionSide == PositionSide.Long ? Common.LongColor : Common.ShortColor;
		[Browsable(false)]
		public SolidColorBrush RealizedPnlColor => RealizedPnl > 0 ? Common.LongColor : RealizedPnl < 0 ? Common.ShortColor : Common.ForegroundColor;

		public BinanceTradeHistory(DateTime time, string symbol, PositionSide positionSide, OrderSide side, decimal price, decimal quantity, decimal quoteQuantity, decimal fee, string feeAsset, decimal realizedPnl, bool isMaker)
		{
			Time = time;
			Symbol = symbol;
			PositionSide = positionSide;
			Side = side;
			Price = price;
			Quantity = quantity;
			QuoteQuantity = quoteQuantity;
			Fee = fee;
			FeeAsset = feeAsset;
			RealizedPnl = realizedPnl;
			IsMaker = isMaker;
		}

		public BinanceTradeHistory(string data)
		{
			var parts = data.Split(',');
			Time = DateTime.Parse(parts[0]);
			Symbol = parts[1];
			PositionSide = (PositionSide)Enum.Parse(typeof(PositionSide), parts[2]);
			Side = (OrderSide)Enum.Parse(typeof(OrderSide), parts[3]);
			Price = decimal.Parse(parts[4]);
			Quantity = decimal.Parse(parts[5]);
			QuoteQuantity = decimal.Parse(parts[6]);
			Fee = decimal.Parse(parts[7]);
			FeeAsset = parts[8];
			RealizedPnl = decimal.Parse(parts[9]);
			IsMaker = parts[10] == "Maker";
		}

		public override string ToString()
		{
			return $"{Time:yyyy-MM-dd HH:mm:ss},{Symbol},{PositionSide},{Side},{Price},{Quantity},{QuoteQuantity},{Fee},{FeeAsset},{RealizedPnl},{(IsMaker ? "Maker" : "Taker")}";
		}
	}
}
