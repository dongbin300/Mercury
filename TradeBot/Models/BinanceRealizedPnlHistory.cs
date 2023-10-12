using System;

namespace TradeBot.Models
{
    public class BinanceRealizedPnlHistory
    {
        public DateTime Time { get; set; }
        public string Symbol { get; set; }
        public double RealizedPnl { get; set; }

        public BinanceRealizedPnlHistory(DateTime time, string symbol, double realizedPnl)
        {
            Time = time;
            Symbol = symbol;
            RealizedPnl = realizedPnl;
        }

        public override string ToString()
        {
            return $"{Time}, {Symbol}, {RealizedPnl} USDT";
        }
    }
}
