using Binance.Net.Enums;

using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Charts
{
	public class ChartInfo(string symbol, Quote quote)
	{
		public string Symbol { get; set; } = symbol;
		public DateTime DateTime => Quote.Date;
		public Quote Quote { get; set; } = quote;
        public decimal Change => Calculator.Roe(PositionSide.Long, Quote.Open, Quote.Close);
        public decimal BodyLength => Math.Abs(Change);
        public CandlestickType CandlestickType => Quote.Open < Quote.Close ? CandlestickType.Bullish : Quote.Open > Quote.Close ? CandlestickType.Bearish : CandlestickType.Doji;

		public double Sma1 { get; set; }
        public double Sma2 { get; set; }
        public double Sma3 { get; set; }
        public double Ema1 { get; set; }
        public double Ema2 { get; set; }
        public double Ema3 { get; set; }
        public double Lsma1 { get; set; }
        public double Lsma2 { get; set; }
        public double Lsma3 { get; set; }
        public double Rsi1 { get; set; }
        public double Rsi2 { get; set; }
        public double Rsi3 { get; set; }
        public double JmaSlope { get; set; }
        public double K { get; set; }
        public double D { get; set; }
        public double Stoch { get; set; }
        public double Supertrend1 { get; set; }
        public double Supertrend2 { get; set; }
        public double Supertrend3 { get; set; }
        public double ReverseSupertrend1 { get; set; }
        public double ReverseSupertrend2 { get; set; }
        public double ReverseSupertrend3 { get; set; }
        public double Macd { get; set; }
        public double MacdSignal { get; set; }
        public double MacdHist { get; set; }
        public double Macd2 { get; set; }
        public double MacdSignal2 { get; set; }
        public double MacdHist2 { get; set; }
        public double Adx { get; set; }
        public double Atr { get; set; }
        public double Bb1Upper { get; set; }
        public double Bb1Lower { get; set; }
        public double Bb2Upper { get; set; }
        public double Bb2Lower { get; set; }

        public double VolumeSma { get; set; }

        public double CustomUpper { get; set; }
        public double CustomLower { get; set; }
        public double CustomPioneer { get; set; }
        public double CustomPlayer { get; set; }

		public double TrendLineUpper { get; set; }
		public double TrendLineLower { get; set; }
		public double TrendRiderTrend { get; set; }
		public double TrendRiderSupertrend { get; set; }

        public double EmaAtrUpper { get; set; }
        public double EmaAtrLower { get; set; }

        public double PredictiveRangesUpper2 { get; set; }
        public double PredictiveRangesUpper { get; set; }
        public double PredictiveRangesAverage { get; set; }
        public double PredictiveRangesLower { get; set; }
        public double PredictiveRangesLower2 { get; set; }

		public override string ToString() => $"{Symbol} | {DateTime} | {Quote.Open} | {Quote.High} | {Quote.Low} | {Quote.Close} | {Quote.Volume}";
	}
}
