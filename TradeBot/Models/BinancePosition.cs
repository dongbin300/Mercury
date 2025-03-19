using Mercury;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinancePosition(string symbol, string positionSide, decimal size, decimal margin, decimal quantity, decimal pnl)
	{
		public string Symbol { get; set; } = symbol;
		public string PositionSide { get; set; } = positionSide;
		public SolidColorBrush PositionSideColor => PositionSide == "Long" ? Common.LongColor : Common.ShortColor;
		/// <summary>
		/// InfoV3, Positions.Notional
		/// Long Position : +
		/// Short Position : -
		/// </summary>
		public decimal Size { get; set; } = Math.Round(size, 3);
		public decimal SizeA => Math.Abs(Size);
		/// <summary>
		/// InfoV3, Positions.InitialMargin
		/// Long Position, Short Position : +
		/// </summary>
		public decimal Margin { get; set; } = Math.Round(margin, 3);
		public int Leverage => (int)Math.Round(SizeA / Margin, 0);
		public decimal Pnl { get; set; } = pnl;
		public string PnlString => GetPnlString();
		public SolidColorBrush PnlColor => Pnl >= 0 ? Common.LongColor : Common.ShortColor;
		//public decimal EntryPrice { get; set; } = entryPrice;
		//public decimal MarkPrice { get; set; } = markPrice;
		public decimal Quantity { get; set; } = quantity;
		public decimal Roe => Math.Round(Pnl / Margin * 100, 2);
		public List<Quote> Quotes => Common.PairQuotes.Find(x => x.Symbol.Equals(Symbol))?.Charts.Select(x => x.Quote).Reverse().ToList() ?? default!;
		public decimal BarPer => Pnl * 20 / Common.Balance;

		public string GetPnlString()
		{
			var builder = new StringBuilder();
			if (Pnl >= 0)
			{
				builder.Append('+');
			}
			builder.Append(Math.Round(Pnl, 3));
			builder.Append(" (");
			if (Roe >= 0)
			{
				builder.Append('+');
			}
			builder.Append(Roe);
			builder.Append("%)");

			return builder.ToString();
		}
	}
}
