using Binance.Net.Enums;

using System;
using System.ComponentModel;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinanceOrderHistory : BinanceHistory
	{
		public DateTime Time { get; set; }
		public DateTime UpdateTime { get; set; }
		public string Symbol { get; set; }
		public PositionSide PositionSide { get; set; }
		public OrderSide Side { get; set; }
		public decimal Price { get; set; }
		public decimal Quantity { get; set; }
		public decimal QuantityFilled { get; set; }
		/// <summary>
		/// Price * QuantityFilled
		/// </summary>
		public decimal? QuoteQuantityFilled { get; set; }
		public OrderStatus Status { get; set; }

		[Browsable(false)]
		public SolidColorBrush PositionSideColor => PositionSide == PositionSide.Long ? Common.LongColor : Common.ShortColor;

		public BinanceOrderHistory(DateTime time, DateTime updateTime, string symbol, PositionSide positionSide, OrderSide side, decimal price, decimal quantity, decimal quantityFilled, decimal? quoteQuantityFilled, OrderStatus status)
		{
			Time = time;
			UpdateTime = updateTime;
			Symbol = symbol;
			PositionSide = positionSide;
			Side = side;
			Price = price;
			Quantity = quantity;
			QuantityFilled = quantityFilled;
			QuoteQuantityFilled = quoteQuantityFilled;
			Status = status;
		}

		public BinanceOrderHistory(string data)
		{
			var parts = data.Split(',');
			Time = DateTime.Parse(parts[0]);
			UpdateTime = DateTime.Parse(parts[1]);
			Symbol = parts[2];
			PositionSide = (PositionSide)Enum.Parse(typeof(PositionSide), parts[3]);
			Side = (OrderSide)Enum.Parse(typeof(OrderSide), parts[4]);
			Price = decimal.Parse(parts[5]);
			Quantity = decimal.Parse(parts[6]);
			QuantityFilled = decimal.Parse(parts[7]);
			QuoteQuantityFilled = decimal.Parse(parts[8]);
			Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), parts[9]);
		}

		public override string ToString()
		{
			return $"{Time:yyyy-MM-dd HH:mm:ss},{UpdateTime:yyyy-MM-dd HH:mm:ss},{Symbol},{PositionSide},{Side},{Price},{Quantity},{QuantityFilled},{QuoteQuantityFilled},{Status}";
		}
	}
}
