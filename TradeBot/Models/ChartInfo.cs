using Binance.Net.Enums;

using Mercury;
using Mercury.Enums;
using Mercury.Maths;

using System;

namespace TradeBot.Models
{
	public class ChartInfo(Quote quote)
	{
		public Quote Quote { get; set; } = quote;
		public double Macd { get; set; }
        public double Signal { get; set; }
        public double Supertrend { get; set; }
        public double Adx { get; set; }
        public double Stoch { get; set; }
        public CandlestickType CandlestickType => Quote.Open < Quote.Close ? CandlestickType.Bullish : Quote.Open > Quote.Close ? CandlestickType.Bearish : CandlestickType.Doji;
		public decimal Change => Calculator.Roe(PositionSide.Long, Quote.Open, Quote.Close);
		public decimal BodyLength => Math.Abs(Change);
	}
}
