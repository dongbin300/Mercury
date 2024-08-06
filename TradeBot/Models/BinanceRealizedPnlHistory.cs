using System;

namespace TradeBot.Models
{
    public class BinanceRealizedPnlHistory(DateTime time, string symbol, decimal realizedPnl)
	{
		public DateTime Time { get; set; } = time;
		public string Symbol { get; set; } = symbol;
		public decimal RealizedPnl { get; set; } = realizedPnl;

		public override string ToString()
        {
            return $"{Time}, {Symbol}, {RealizedPnl} USDT";
        }
    }
}
