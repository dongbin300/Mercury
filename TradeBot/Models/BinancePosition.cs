using Binance.Net.Enums;

using System;
using System.Text;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinancePosition(string symbol, string positionSide, decimal pnl, decimal quantity, decimal margin)
	{
		public string Symbol { get; set; } = symbol;
		public string PositionSide { get; set; } = positionSide;
		public SolidColorBrush PositionSideColor => PositionSide == "Long" ? Common.LongColor : Common.ShortColor;
		public decimal Pnl { get; set; } = pnl;
		public string PnlString => GetPnlString();
        public SolidColorBrush PnlColor => Pnl >= 0 ? Common.LongColor : Common.ShortColor;
		//public decimal EntryPrice { get; set; } = entryPrice;
		//public decimal MarkPrice { get; set; } = markPrice;
		public decimal Quantity { get; set; } = quantity;
        public decimal Margin { get; set; } = Math.Round(margin, 3);
        public decimal Roe => Math.Round(Pnl / Margin * 100, 2);

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
            builder.Append(Roe);
            builder.Append("%)");

            return builder.ToString();
        }
    }
}
