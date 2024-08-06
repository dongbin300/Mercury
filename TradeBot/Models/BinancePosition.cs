using System;
using System.Text;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinancePosition(string symbol, string positionSide, decimal pnl, decimal entryPrice, decimal markPrice, decimal quantity, int leverage)
	{
		public string Symbol { get; set; } = symbol;
		public string PositionSide { get; set; } = positionSide;
		public SolidColorBrush PositionSideColor => PositionSide == "Long" ? Common.LongColor : Common.ShortColor;
		public decimal Pnl { get; set; } = pnl;
		public string PnlString => GetPnlString();
        public SolidColorBrush PnlColor => Pnl >= 0 ? Common.LongColor : Common.ShortColor;
		public decimal EntryPrice { get; set; } = entryPrice;
		public decimal MarkPrice { get; set; } = markPrice;
		public decimal Quantity { get; set; } = quantity;
		public int Leverage { get; set; } = leverage;
		public decimal Margin => Math.Round(Math.Abs(MarkPrice * Quantity / Leverage), 3);
        public decimal Roe => Math.Round(Pnl / Math.Abs(MarkPrice * Quantity / Leverage) * 100, 2);

		public string GetPnlString()
        {
            var builder = new StringBuilder();
            if(Pnl >= 0)
            {
                builder.Append('+');
            }
            builder.Append(Math.Round(Pnl, 3));
            builder.Append(" (");
            if(Roe >= 0)
            {
                builder.Append('+');
            }
            builder.Append(Math.Round(Pnl / Math.Abs(MarkPrice * Quantity / Leverage) * 100, 2));
            builder.Append("%)");

            return builder.ToString();
        }
    }
}
