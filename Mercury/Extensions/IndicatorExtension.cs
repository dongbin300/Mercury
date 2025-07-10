using Mercury.Indicators;
using Mercury.Maths;

namespace Mercury.Extensions
{
	public enum QuoteType
	{
		Open,
		High,
		Low,
		Close
	}

	public static class IndicatorExtension
	{
		/// <summary>
		/// Convert to Heikin Ashi candle
		/// First Open
		/// = Candle(0).Open
		/// N Open
		/// = (Candle(-1).Open + Candle(-1).Close) / 2
		/// 
		/// Close
		/// = (Candle(0).Open + Candle(0).High + Candle(0).Low + Candle(0).Close) / 4
		/// 
		/// High
		/// = Max(Candle(0).High, Candle(0).Open, Candle(0).Close)
		/// 
		/// Low
		/// = Min(Candle(0).Low, Candle(0).Open, Candle(0).Close)
		/// </summary>
		/// <param name="quotes"></param>
		/// <returns></returns>
		public static IEnumerable<Quote> GetHeikinAshiCandle(this IEnumerable<Quote> quotes)
		{
			var result = new List<Quote>();

			var _q = quotes.ElementAt(0);
			result.Add(new Quote(
				_q.Date,
				(_q.Open + _q.Close) / 2,
				Math.Max(_q.High, Math.Max(_q.Open, _q.Close)),
				Math.Min(_q.Low, Math.Min(_q.Open, _q.Close)),
				(_q.Open + _q.High + _q.Low + _q.Close) / 4
				));

			for (int i = 1; i < quotes.Count(); i++)
			{
				var q = quotes.ElementAt(i);
				var prevHa = result[i - 1];
				result.Add(new Quote(
					q.Date,
					(prevHa.Open + prevHa.Close) / 2,
					Math.Max(q.High, Math.Max(q.Open, q.Close)),
					Math.Min(q.Low, Math.Min(q.Open, q.Close)),
					(q.Open + q.High + q.Low + q.Close) / 4
				));
			}

			return result;
		}

		public static IEnumerable<RiResult> GetRi(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<RiResult>();
			var quoteList = quotes.ToList();

			result.Add(new RiResult(quoteList[0].Date, 0));
			for (int i = 1; i < quoteList.Count; i++)
			{
				double sum = 0;
				var _period = Math.Min(period, i);
				for (int j = 0; j < _period; j++)
				{
					sum += Convert.ToDouble(quoteList[i - j].Close);
				}
				var average = sum / _period;
				var ri = (Convert.ToDouble(quoteList[i].Close) - average) / average * 1000;
				result.Add(new RiResult(quoteList[i].Date, (decimal)ri));
			}

			return result;
		}

		public static IEnumerable<LsmaResult> GetLsma(this IEnumerable<Quote> quotes, int period)
		{
			var pl = new List<double>();
			var result = new List<LsmaResult>();
			var quoteList = quotes.ToList();

			for (int i = 0; i < quoteList.Count; i++)
			{
				pl.Add((double)quoteList[i].Close);

				if (pl.Count >= period)
				{
					double sumX = 0;
					double sumY = 0;
					double sumXY = 0;
					double sumXX = 0;
					double sumYY = 0;

					for (int a = 1; a <= pl.Count; a++)
					{
						sumX += a;
						sumY += pl[a - 1];
						sumXY += pl[a - 1] * a;
						sumXX += a * a;
						sumYY += pl[a - 1] * pl[a - 1];
					}

					double m = (sumXY - sumX * sumY / period) / (sumXX - sumX * sumX / period);
					double b = sumY / period - m * sumX / period;
					result.Add(new LsmaResult(quoteList[i].Date, (decimal)(m * period + b)));
					pl = [.. pl.Skip(1)];
				}
				else
				{
					result.Add(new LsmaResult(quoteList[i].Date, 0));
				}
			}

			return result;
		}

		public static IEnumerable<RsiResult> GetRsi(this IEnumerable<Quote> quotes, int period = 14)
		{
			var result = new List<RsiResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray();
			var rsi = ArrayCalculator.Rsi(values, period);
			for (int i = 0; i < rsi.Length; i++)
			{
				result.Add(new RsiResult(quotes.ElementAt(i).Date, (decimal?)rsi[i]));
			}

			return result;
		}

		public static IEnumerable<StochResult> GetStoch(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<StochResult>();

			var high = quotes.Select(x => (double)x.High).ToArray().ToNullable();
			var low = quotes.Select(x => (double)x.Low).ToArray().ToNullable();
			var close = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var k = ArrayCalculator.Stoch(high, low, close, period);
			for (int i = 0; i < k.Length; i++)
			{
				result.Add(new StochResult(quotes.ElementAt(i).Date, (decimal?)k[i]));
			}

			return result;
		}

		public static IEnumerable<StochasticRsiResult> GetStochasticRsi(this IEnumerable<Quote> quotes, int smoothK = 3, int smoothD = 3, int rsiPeriod = 14, int stochasticPeriod = 14)
		{
			var result = new List<StochasticRsiResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray();
			(var k, var d) = ArrayCalculator.StochasticRsi(values, smoothK, smoothD, rsiPeriod, stochasticPeriod);
			for (int i = 0; i < k.Length; i++)
			{
				result.Add(new StochasticRsiResult(quotes.ElementAt(i).Date, (decimal?)k[i], (decimal?)d[i]));
			}

			return result;
		}

		public static IEnumerable<StdevResult> GetStdev(this IEnumerable<Quote> quotes, int period = 20)
		{
			var result = new List<StdevResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray();
			var stdev = ArrayCalculator.Stdev(values.ToNullable(), period);
			for (int i = 0; i < stdev.Length; i++)
			{
				result.Add(new StdevResult(quotes.ElementAt(i).Date, (decimal?)stdev[i]));
			}

			return result;
		}

		public static IEnumerable<SmaResult> GetSma(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<SmaResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var sma = ArrayCalculator.Sma(values, period);
			for (int i = 0; i < sma.Length; i++)
			{
				result.Add(new SmaResult(quotes.ElementAt(i).Date, (decimal?)sma[i]));
			}

			return result;
		}

		public static IEnumerable<EmaResult> GetEma(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<EmaResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var ema = ArrayCalculator.Ema(values, period);
			for (int i = 0; i < ema.Length; i++)
			{
				result.Add(new EmaResult(quotes.ElementAt(i).Date, (decimal?)ema[i]));
			}

			return result;
		}

		public static IEnumerable<EwmacResult> GetEwmac(this IEnumerable<Quote> quotes, int shortPeriod, int longPeriod)
		{
			var result = new List<EwmacResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var ewmac = ArrayCalculator.Ewmac(values, shortPeriod, longPeriod);
			for (int i = 0; i < ewmac.Length; i++)
			{
				result.Add(new EwmacResult(quotes.ElementAt(i).Date, (decimal?)ewmac[i]));
			}

			return result;
		}

		public static IEnumerable<VolatilityRatioResult> GetVolatilityRatio(this IEnumerable<Quote> quotes, int currentPeriod, int longPeriod)
		{
			var result = new List<VolatilityRatioResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var volatilityRatio = ArrayCalculator.VolatilityRatio(values, currentPeriod, longPeriod);
			for (int i = 0; i < volatilityRatio.Length; i++)
			{
				result.Add(new VolatilityRatioResult(quotes.ElementAt(i).Date, (decimal?)volatilityRatio[i]));
			}
			return result;
		}

		public static IEnumerable<SmaResult> GetVolumeSma(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<SmaResult>();

			var values = quotes.Select(x => (double)x.Volume).ToArray().ToNullable();
			var sma = ArrayCalculator.Sma(values, period);
			for (int i = 0; i < sma.Length; i++)
			{
				result.Add(new SmaResult(quotes.ElementAt(i).Date, (decimal?)sma[i]));
			}

			return result;
		}

		public static IEnumerable<BbResult> GetBollingerBands(this IEnumerable<Quote> quotes, int period = 20, double deviation = 2.0, QuoteType quoteType = QuoteType.Close)
		{
			var result = new List<BbResult>();

			var values = quoteType switch
			{
				QuoteType.Open => quotes.Select(x => (double)x.Open).ToArray().ToNullable(),
				QuoteType.High => quotes.Select(x => (double)x.High).ToArray().ToNullable(),
				QuoteType.Low => quotes.Select(x => (double)x.Low).ToArray().ToNullable(),
				QuoteType.Close or _ => quotes.Select(x => (double)x.Close).ToArray().ToNullable(),
			};

			(var sma, var upper, var lower) = ArrayCalculator.BollingerBands(values, period, deviation);
			for (int i = 0; i < sma.Length; i++)
			{
				result.Add(new BbResult(quotes.ElementAt(i).Date, (decimal?)sma[i], (decimal?)upper[i], (decimal?)lower[i]));
			}

			return result;
		}

		public static IEnumerable<IchimokuCloudResult> GetIchimokuCloud(this IEnumerable<Quote> quotes, int conversionPeriod = 9, int basePeriod = 26, int leadingSpanPeriod = 52)
		{
			var result = new List<IchimokuCloudResult>();

			var interval = quotes.ElementAt(1).Date - quotes.ElementAt(0).Date;
			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var conversion, var _base, var trailingSpan, var leadingSpan1, var leadingSpan2) = ArrayCalculator.IchimokuCloud(high, low, close, conversionPeriod, basePeriod, leadingSpanPeriod);
			for (int i = 0; i < high.Length; i++)
			{
				result.Add(new IchimokuCloudResult(quotes.ElementAt(i).Date, (decimal?)conversion[i], (decimal?)_base[i], (decimal?)trailingSpan[i], (decimal?)leadingSpan1[i], (decimal?)leadingSpan2[i]));
			}
			// TODO : value sync to time
			var nextDateTime = result[^1].Date;
			for (int i = high.Length; i < high.Length + basePeriod - 1; i++)
			{
				nextDateTime += interval;
				result.Add(new IchimokuCloudResult(nextDateTime, 0, 0, 0, (decimal?)leadingSpan1[i], (decimal?)leadingSpan2[i]));
			}

			return result;
		}

		public static IEnumerable<MacdResult> GetMacd(this IEnumerable<Quote> quotes, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
		{
			var result = new List<MacdResult>();

			var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			(var macd, var signal, var hist) = ArrayCalculator.Macd(values, fastPeriod, slowPeriod, signalPeriod);
			for (int i = 0; i < macd.Length; i++)
			{
				result.Add(new MacdResult(quotes.ElementAt(i).Date, (decimal?)macd[i], (decimal?)signal[i], (decimal?)hist[i]));
			}

			return result;
		}

		public static IEnumerable<CciResult> GetCci(this IEnumerable<Quote> quotes, int period = 20)
		{
			var result = new List<CciResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			var cci = ArrayCalculator.Cci(high, low, close, period);
			for (int i = 0; i < cci.Length; i++)
			{
				result.Add(new CciResult(quotes.ElementAt(i).Date, (decimal?)cci[i]));
			}

			return result;
		}

		public static IEnumerable<SupertrendResult> GetSupertrend(this IEnumerable<Quote> quotes, int atrPeriod, double factor)
		{
			var result = new List<SupertrendResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var supertrend, var direction) = ArrayCalculator.Supertrend(high, low, close, atrPeriod, factor);

			for (int i = 0; i < supertrend.Length; i++)
			{
				var st = i >= atrPeriod - 1 ? -direction[i] * supertrend[i] : 0;
				var _supertrend = new SupertrendResult(quotes.ElementAt(i).Date, (decimal?)st);
				result.Add(_supertrend);
			}

			return result;
		}

		public static IEnumerable<SupertrendResult> GetReverseSupertrend(this IEnumerable<Quote> quotes, int atrPeriod, double factor)
		{
			var result = new List<SupertrendResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var supertrend, var direction) = ArrayCalculator.ReverseSupertrend(high, low, close, atrPeriod, factor);

			for (int i = 0; i < supertrend.Length; i++)
			{
				var st = i >= atrPeriod - 1 ? -direction[i] * supertrend[i] : 0;
				var _supertrend = new SupertrendResult(quotes.ElementAt(i).Date, (decimal?)st);
				result.Add(_supertrend);
			}

			return result;
		}

		public static IEnumerable<TripleSupertrendResult> GetTripleSupertrend(this IEnumerable<Quote> quotes, int atrPeriod1, double factor1, int atrPeriod2, double factor2, int atrPeriod3, double factor3)
		{
			var result = new List<TripleSupertrendResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var supertrend1, var direction1, var supertrend2, var direction2, var supertrend3, var direction3) = ArrayCalculator.TripleSupertrend(high, low, close, atrPeriod1, factor1, atrPeriod2, factor2, atrPeriod3, factor3);

			for (int i = 0; i < supertrend1.Length; i++)
			{
				var st1 = i >= atrPeriod1 - 1 ? -direction1[i] * supertrend1[i] : 0;
				var st2 = i >= atrPeriod2 - 1 ? -direction2[i] * supertrend2[i] : 0;
				var st3 = i >= atrPeriod3 - 1 ? -direction3[i] * supertrend3[i] : 0;
				var tripleSupertrend = new TripleSupertrendResult(quotes.ElementAt(i).Date, (decimal?)st1, (decimal?)st2, (decimal?)st3);
				result.Add(tripleSupertrend);
			}

			return result;
		}

		public static IEnumerable<AdxResult> GetAdx(this IEnumerable<Quote> quotes, int adxPeriod = 14, int diPeriod = 14)
		{
			var result = new List<AdxResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			var adx = ArrayCalculator.Adx(high, low, close, adxPeriod, diPeriod);

			for (int i = 0; i < adx.Length; i++)
			{
				result.Add(new AdxResult(quotes.ElementAt(i).Date, (decimal?)adx[i]));
			}

			return result;
		}

		public static IEnumerable<AtrResult> GetAtr(this IEnumerable<Quote> quotes, int period = 14)
		{
			var result = new List<AtrResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			var atr = ArrayCalculator.Atr(high, low, close, period);

			for (int i = 0; i < atr.Length; i++)
			{
				result.Add(new AtrResult(quotes.ElementAt(i).Date, (decimal?)atr[i]));
			}

			return result;
		}

		public static IEnumerable<TsvResult> GetTsv(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<TsvResult>();

			var close = quotes.Select(x => (double)x.Close).ToArray();
			var volume = quotes.Select(x => (double)x.Volume).ToArray();
			var tsv = ArrayCalculator.TimeSegmentedVolume(close, volume, period);

			for (int i = 0; i < tsv.Length; i++)
			{
				result.Add(new TsvResult(quotes.ElementAt(i).Date, (decimal?)tsv[i]));
			}

			return result;
		}

		public static IEnumerable<CustomResult> GetCustom(this IEnumerable<Quote> quotes, int period)
		{
			var result = new List<CustomResult>();

			var open = quotes.Select(x => (double)x.Open).ToArray();
			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			var volume = quotes.Select(x => (double)x.Volume).ToArray();
			(var upper, var lower, var pioneer, var player) = ArrayCalculator.Custom(open, high, low, close, volume, period);

			for (int i = 0; i < upper.Length; i++)
			{
				result.Add(new CustomResult(quotes.ElementAt(i).Date, (decimal?)upper[i], (decimal?)lower[i], (decimal?)pioneer[i], (decimal?)player[i]));
			}

			return result;
		}

		public static IEnumerable<TrendRiderResult> GetTrendRider(this IEnumerable<Quote> quotes, int atrPeriod = 10, double atrMultiplier = 3.0, int rsiPeriod = 14, int macdFastPeriod = 12, int macdSlowPeriod = 26, int macdSignalPeriod = 9)
		{
			var result = new List<TrendRiderResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var trend, var supertrend, var supertrendDirection) = ArrayCalculator.TrendRider(high, low, close, atrPeriod, atrMultiplier, rsiPeriod, macdFastPeriod, macdSlowPeriod, macdSignalPeriod);

			for (int i = 0; i < trend.Length; i++)
			{
				var st = i >= atrPeriod - 1 ? -supertrendDirection[i] * supertrend[i] : 0;
				var trendRider = new TrendRiderResult(quotes.ElementAt(i).Date, -(decimal?)trend[i], (decimal?)st);
				result.Add(trendRider);
			}

			return result;
		}

		public static IEnumerable<PredictiveRangesResult> GetPredictiveRanges(this IEnumerable<Quote> quotes, int period = 200, double factor = 6.0)
		{
			var result = new List<PredictiveRangesResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var upper2, var upper, var average, var lower, var lower2) = ArrayCalculator.PredictiveRanges(high, low, close, period, factor);

			for (int i = 0; i < upper2.Length; i++)
			{
				var predictiveRanges = new PredictiveRangesResult(quotes.ElementAt(i).Date, (decimal?)upper2[i], (decimal?)upper[i], (decimal?)average[i], (decimal?)lower[i], (decimal?)lower2[i]);
				result.Add(predictiveRanges);
			}

			return result;
		}

		public static IEnumerable<MercuryRangesResult> GetMercuryRanges(this IEnumerable<Quote> quotes, int period = 200, double factor = 6.0)
		{
			var result = new List<MercuryRangesResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var upper, var average, var lower) = ArrayCalculator.MercuryRanges(high, low, close, period, factor);

			for (int i = 0; i < upper.Length; i++)
			{
				var mercuryRanges = new MercuryRangesResult(quotes.ElementAt(i).Date, (decimal?)upper[i], (decimal?)average[i], (decimal?)lower[i]);
				result.Add(mercuryRanges);
			}

			return result;
		}

		public static IEnumerable<MlmipResult> GetMlmip(this IEnumerable<Quote> quotes, int pivotBars = 20, int momentumWindow = 25, int maxData = 500, int numNeighbors = 100, int predictionSmoothing = 20)
		{
			var result = new List<MlmipResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var prediction, var predictionMa) = ArrayCalculator.Mlmip(high, low, close, pivotBars, momentumWindow, maxData, numNeighbors, predictionSmoothing);

			for (int i = 0; i < prediction.Length; i++)
			{
				var mlmip = new MlmipResult(quotes.ElementAt(i).Date, (decimal?)prediction[i], (decimal?)predictionMa[i]);
				result.Add(mlmip);
			}

			return result;
		}

		public static IEnumerable<AtrmaResult> GetAtrma(this IEnumerable<Quote> quotes, int atrPeriod = 14, int maPeriod = 20)
		{
			var result = new List<AtrmaResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			var atrma = ArrayCalculator.Atrma(high, low, close, atrPeriod, maPeriod);

			for (int i = 0; i < atrma.Length; i++)
			{
				result.Add(new AtrmaResult(quotes.ElementAt(i).Date, (decimal?)atrma[i]));
			}

			return result;
		}

		public static IEnumerable<DonchianChannelResult> GetDonchianChannel(this IEnumerable<Quote> quotes, int period = 20)
		{
			var result = new List<DonchianChannelResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			(var basis, var upper, var lower) = ArrayCalculator.DonchianChannel(high, low, period);

			for (int i = 0; i < basis.Length; i++)
			{
				result.Add(new DonchianChannelResult(quotes.ElementAt(i).Date, (decimal?)basis[i], (decimal?)upper[i], (decimal?)lower[i]));
			}

			return result;
		}

		public static IEnumerable<SqueezeMomentumResult> GetSqueezeMomentum(this IEnumerable<Quote> quotes, int bbPeriod = 20, double bbFactor = 2.0, int kcPeriod = 20, double kcFactor = 1.5, bool useTrueRange = true)
		{
			var result = new List<SqueezeMomentumResult>();

			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			(var value, var direction, var signal) = ArrayCalculator.SqueezeMomentum(high, low, close, bbPeriod, bbFactor, kcPeriod, kcFactor, useTrueRange);

			for (int i = 0; i < value.Length; i++)
			{
				result.Add(new SqueezeMomentumResult(quotes.ElementAt(i).Date, (decimal?)value[i], direction[i], signal[i]));
			}

			return result;
		}

		public static IEnumerable<CandleScoreResult> GetCandleScore(this IEnumerable<Quote> quotes)
		{
			var result = new List<CandleScoreResult>();

			var open = quotes.Select(x => (double)x.Open).ToArray();
			var high = quotes.Select(x => (double)x.High).ToArray();
			var low = quotes.Select(x => (double)x.Low).ToArray();
			var close = quotes.Select(x => (double)x.Close).ToArray();
			var candleScore = ArrayCalculator.CandleScore(open, high, low, close, null);

			for (int i = 0; i < candleScore.Length; i++)
			{
				result.Add(new CandleScoreResult(quotes.ElementAt(i).Date, (decimal?)candleScore[i]));
			}

			return result;
		}

		public static IEnumerable<VwapResult> GetVwap(this IEnumerable<Quote> quotes)
		{
			var result = new List<VwapResult>();

			var high = quotes.Select(x => (double)x.High).ToArray().ToNullable();
			var low = quotes.Select(x => (double)x.Low).ToArray().ToNullable();
			var close = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var volume = quotes.Select(x => (double)x.Volume).ToArray().ToNullable();
			var vwap = ArrayCalculator.Vwap(high, low, close, volume);

			for (int i = 0; i < vwap.Length; i++)
			{
				result.Add(new VwapResult(quotes.ElementAt(i).Date, (decimal?)vwap[i]));
			}

			return result;
		}

		public static IEnumerable<RollingVwapResult> GetRollingVwap(this IEnumerable<Quote> quotes, int period = 20)
		{
			var result = new List<RollingVwapResult>();

			var high = quotes.Select(x => (double)x.High).ToArray().ToNullable();
			var low = quotes.Select(x => (double)x.Low).ToArray().ToNullable();
			var close = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var volume = quotes.Select(x => (double)x.Volume).ToArray().ToNullable();
			var rollingVwap = ArrayCalculator.RollingVwap(high, low, close, volume, period);

			for (int i = 0; i < rollingVwap.Length; i++)
			{
				result.Add(new RollingVwapResult(quotes.ElementAt(i).Date, (decimal?)rollingVwap[i]));
			}

			return result;
		}

		public static IEnumerable<EvwapResult> GetEvwap(this IEnumerable<Quote> quotes, int period = 13)
		{
			var result = new List<EvwapResult>();

			var high = quotes.Select(x => (double)x.High).ToArray().ToNullable();
			var low = quotes.Select(x => (double)x.Low).ToArray().ToNullable();
			var close = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var volume = quotes.Select(x => (double)x.Volume).ToArray().ToNullable();
			var evwap = ArrayCalculator.Evwap(high, low, close, volume, period);

			for (int i = 0; i < evwap.Length; i++)
			{
				result.Add(new EvwapResult(quotes.ElementAt(i).Date, (decimal?)evwap[i]));
			}

			return result;
		}

		public static IEnumerable<ElderRayPowerResult> GetElderRayPower(this IEnumerable<Quote> quotes, int emaPeriod = 13)
		{
			var result = new List<ElderRayPowerResult>();

			var high = quotes.Select(x => (double)x.High).ToArray().ToNullable();
			var low = quotes.Select(x => (double)x.Low).ToArray().ToNullable();
			var close = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
			var volume = quotes.Select(x => (double)x.Volume).ToArray().ToNullable();
			(var bullPower, var bearPower) = ArrayCalculator.ElderRayPower(high, low, close, emaPeriod);

			for (int i = 0; i < bullPower.Length; i++)
			{
				result.Add(new ElderRayPowerResult(quotes.ElementAt(i).Date, (decimal?)bullPower[i], (decimal?)bearPower[i]));
			}

			return result;
		}
	}
}
