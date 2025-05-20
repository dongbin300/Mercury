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

		public double? Stdev1 { get; set; }
		public double? Sma1 { get; set; }
		public double? Sma2 { get; set; }
		public double? Sma3 { get; set; }
		public double? Ema1 { get; set; }
		public double? Ema2 { get; set; }
		public double? Ema3 { get; set; }
		public double? Ewmac { get; set; }
		public double? Lsma1 { get; set; }
		public double? Lsma2 { get; set; }
		public double? Lsma3 { get; set; }
		public double? Rsi1 { get; set; }
		public double? Rsi2 { get; set; }
		public double? Rsi3 { get; set; }
		public double? JmaSlope { get; set; }
		public double? K { get; set; }
		public double? D { get; set; }
		public double? Stoch { get; set; }
		public double? StochK { get; set; }
		public double? StochD { get; set; }
		public double? Cci { get; set; }
		public double? Supertrend1 { get; set; }
		public double? Supertrend2 { get; set; }
		public double? Supertrend3 { get; set; }
		public double? ReverseSupertrend1 { get; set; }
		public double? ReverseSupertrend2 { get; set; }
		public double? ReverseSupertrend3 { get; set; }
		public double? Macd { get; set; }
		public double? MacdSignal { get; set; }
		public double? MacdHist { get; set; }
		public double? Macd2 { get; set; }
		public double? MacdSignal2 { get; set; }
		public double? MacdHist2 { get; set; }
		public double? Adx { get; set; }
		public double? Atr { get; set; }
		public double? Atrma { get; set; }
		public double? Bb1Sma { get; set; }
		public double? Bb1Upper { get; set; }
		public double? Bb1Lower { get; set; }
		public double? Bb2Sma { get; set; }
		public double? Bb2Upper { get; set; }
		public double? Bb2Lower { get; set; }

		public double? VolumeSma { get; set; }

		public double? CustomUpper { get; set; }
		public double? CustomLower { get; set; }
		public double? CustomPioneer { get; set; }
		public double? CustomPlayer { get; set; }

		public double? TrendLineUpper { get; set; }
		public double? TrendLineLower { get; set; }
		public double? TrendRiderTrend { get; set; }
		public double? TrendRiderSupertrend { get; set; }

		public double? EmaAtrUpper { get; set; }
		public double? EmaAtrLower { get; set; }

		public double? PredictiveRangesUpper2 { get; set; }
		public double? PredictiveRangesUpper { get; set; }
		public double? PredictiveRangesAverage { get; set; }
		public double? PredictiveRangesLower { get; set; }
		public double? PredictiveRangesLower2 { get; set; }
		public double? PredictiveRangesMaxLeverage { get; set; }

		public double? Prediction { get; set; }
		public double? PredictionMa { get; set; }

		public double? DcBasis { get; set; }
		public double? DcUpper { get; set; }
		public double? DcLower { get; set; }

		public double? MarketScore { get; set; }

		public double? SmValue { get; set; }
		public int? SmDirection { get; set; }
		public int? SmSignal { get; set; }

		public double? VolatilityRatio { get; set; }

		public double? CandleScore { get; set; }

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
