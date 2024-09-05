using Mercury;

using System;
using System.Windows.Media;

namespace TradeBot.Models
{
    public class BinanceRealizedPnlHistory(DateTime time, string symbol, decimal realizedPnl)
	{
		public DateTime Time { get; set; } = time;
		public string _Time => Time.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss");
		public string Symbol { get; set; } = symbol;
		public decimal RealizedPnl { get; set; } = realizedPnl.Round(6);
		public SolidColorBrush PnlColor => RealizedPnl >= 0 ? Common.LongColor : Common.ShortColor;

		public override string ToString()
        {
            return $"{Time}, {Symbol}, {RealizedPnl} USDT";
        }
    }
}
