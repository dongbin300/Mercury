using Binance.Net.Enums;

using Mercury.Assets;
using Mercury.Elements;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Charts
{
	public class ChartInfo(string symbol, Quote quote)
	{
		public string Symbol { get; set; } = symbol;
		public string BaseAsset => Symbol.Replace("USDT", "");
		public DateTime DateTime => Quote.Date;
		public Quote Quote { get; set; } = quote;
		public decimal Change => Calculator.Roe(PositionSide.Long, Quote.Open, Quote.Close);
		public decimal BodyLength => Math.Abs(Change);
		public decimal UpTailLength => Calculator.Roe(PositionSide.Long, Math.Max(Quote.Open, Quote.Close), Quote.High);
		public decimal DownTailLength => Calculator.Roe(PositionSide.Long, Quote.Low, Math.Min(Quote.Open, Quote.Close));
		public CandlestickType CandlestickType => Quote.Open < Quote.Close ? CandlestickType.Bullish : Quote.Open > Quote.Close ? CandlestickType.Bearish : CandlestickType.Doji;
		public IList<ChartElementResult> ChartElements { get; set; } = [];
		public IList<NamedElementResult> NamedElements { get; set; } = [];

		public decimal? Stdev1 { get; set; }
		public decimal? Sma1 { get; set; }
		public decimal? Sma2 { get; set; }
		public decimal? Sma3 { get; set; }
		public decimal? Sma4 { get; set; }
		public decimal? Sma5 { get; set; }
		public decimal? Sma6 { get; set; }
		public decimal? Sma7 { get; set; }
		public decimal? Sma8 { get; set; }
		public decimal? Sma9 { get; set; }
		public decimal? Sma10 { get; set; }
		public decimal? Ema1 { get; set; }
		public decimal? Ema2 { get; set; }
		public decimal? Ema3 { get; set; }
		public decimal? Ema4 { get; set; }
		public decimal? Ema5 { get; set; }
		public decimal? Ema6 { get; set; }
		public decimal? Ema7 { get; set; }
		public decimal? Ema8 { get; set; }
		public decimal? Ema9 { get; set; }
		public decimal? Ema10 { get; set; }
		public decimal? Ewmac { get; set; }
		public decimal? Lsma1 { get; set; }
		public decimal? Lsma2 { get; set; }
		public decimal? Lsma3 { get; set; }
		public decimal? Rsi1 { get; set; }
		public decimal? Rsi2 { get; set; }
		public decimal? Rsi3 { get; set; }
		public decimal? JmaSlope { get; set; }
		public decimal? K { get; set; }
		public decimal? D { get; set; }
		public decimal? Stoch { get; set; }
		public decimal? StochK { get; set; }
		public decimal? StochD { get; set; }
		public decimal? Cci { get; set; }
		public decimal? Supertrend1 { get; set; }
		public decimal? Supertrend2 { get; set; }
		public decimal? Supertrend3 { get; set; }
		public decimal? ReverseSupertrend1 { get; set; }
		public decimal? ReverseSupertrend2 { get; set; }
		public decimal? ReverseSupertrend3 { get; set; }
		public decimal? Macd { get; set; }
		public decimal? MacdSignal { get; set; }
		public decimal? MacdHist { get; set; }
		public decimal? Macd2 { get; set; }
		public decimal? MacdSignal2 { get; set; }
		public decimal? MacdHist2 { get; set; }
		public decimal? Adx { get; set; }
		public decimal? Atr { get; set; }
		public decimal? Atrma { get; set; }
		public decimal? Bb1Sma { get; set; }
		public decimal? Bb1Upper { get; set; }
		public decimal? Bb1Lower { get; set; }
		public decimal? Bb2Sma { get; set; }
		public decimal? Bb2Upper { get; set; }
		public decimal? Bb2Lower { get; set; }

		public decimal? VolumeSma { get; set; }

		public decimal? CustomUpper { get; set; }
		public decimal? CustomLower { get; set; }
		public decimal? CustomPioneer { get; set; }
		public decimal? CustomPlayer { get; set; }

		public decimal? TrendLineUpper { get; set; }
		public decimal? TrendLineLower { get; set; }
		public decimal? TrendRiderTrend { get; set; }
		public decimal? TrendRiderSupertrend { get; set; }

		public decimal? EmaAtrUpper { get; set; }
		public decimal? EmaAtrLower { get; set; }

		public decimal? PredictiveRangesUpper2 { get; set; }
		public decimal? PredictiveRangesUpper { get; set; }
		public decimal? PredictiveRangesAverage { get; set; }
		public decimal? PredictiveRangesLower { get; set; }
		public decimal? PredictiveRangesLower2 { get; set; }
		public decimal? PredictiveRangesMaxLeverage { get; set; }

		public decimal? MercuryRangesUpper { get; set; }
		public decimal? MercuryRangesAverage { get; set; }
		public decimal? MercuryRangesLower { get; set; }

		public decimal? Prediction { get; set; }
		public decimal? PredictionMa { get; set; }

		public decimal? DcBasis { get; set; }
		public decimal? DcUpper { get; set; }
		public decimal? DcLower { get; set; }

		public decimal? MarketScore { get; set; }

		public decimal? SmValue { get; set; }
		public int? SmDirection { get; set; }
		public int? SmSignal { get; set; }

		public decimal? Vwap { get; set; }
		public decimal? RollingVwap { get; set; }
		public decimal? Evwap { get; set; }

		public decimal? ElderRayBullPower { get; set; }
		public decimal? ElderRayBearPower { get; set; }

		public decimal? VolatilityRatio { get; set; }

		public decimal? CandleScore { get; set; }

		public decimal LiquidationPriceLong { get; set; } = 0m;
		public decimal LiquidationPriceShort { get; set; } = 0m;

		public override string ToString() => $"{Symbol} | {DateTime} | {Quote.Open} | {Quote.High} | {Quote.Low} | {Quote.Close} | {Quote.Volume}";

		public string ToElementString() => $"{Symbol}, {DateTime}, {Quote.Open}:{Quote.High}:{Quote.Low}:{Quote.Close}:{Quote.Volume}, {string.Join(',', ChartElements)}, {string.Join(',', NamedElements)}";

		public ChartElementResult? GetChartElementResult(MtmChartElementType type) => ChartElements.FirstOrDefault(x => x != null && x.Type.Equals(type), null);
		public decimal? GetChartElementValue(MtmChartElementType type) => type switch
		{
			MtmChartElementType.candle_open => Quote.Open,
			MtmChartElementType.candle_high => Quote.High,
			MtmChartElementType.candle_low => Quote.Low,
			MtmChartElementType.candle_close => Quote.Close,
			MtmChartElementType.volume => Quote.Volume,
			_ => GetChartElementResult(type)?.Value,
		};
		public NamedElementResult? GetNamedElementResult(string name) => NamedElements.FirstOrDefault(x => x != null && x.Name.Equals(name), null);
		public decimal? GetNamedElementValue(string name) => GetNamedElementResult(name)?.Value;
		public decimal? GetTradeElementValue(Asset asset, TradeElement tradeElement) => tradeElement.ElementType switch
		{
			MtmTradeElementType.roe => asset.Position.AveragePrice * (1 + (tradeElement.Value / 100)),
			_ => tradeElement.Value
		};
	}
}
