using Binance.Net.Enums;

using Mercury.Extensions;

namespace Mercury.Charts
{
	public class ChartPack(KlineInterval interval)
	{
		public string Symbol => Charts.First().Symbol;
		public KlineInterval Interval = interval;
		public IList<ChartInfo> Charts { get; set; } = [];
		public DateTime StartTime => Charts.Min(x => x.DateTime);
		public DateTime EndTime => Charts.Max(x => x.DateTime);
		public ChartInfo? CurrentChart = null;

		public void AddChart(ChartInfo chart)
		{
			Charts.Add(chart);
		}

		public void ConvertCandle()
		{
			if (Interval == KlineInterval.OneMinute || Interval == KlineInterval.FiveMinutes || Interval == KlineInterval.OneHour || Interval == KlineInterval.OneDay)
			{
				return;
			}

			var newCharts = new List<ChartInfo>();
			var groupedByInterval = Charts.GroupBy(c => GetGroupingKey(c.DateTime, Interval));

			foreach (var group in groupedByInterval)
			{
				var groupCharts = group.ToList();
				var firstChart = groupCharts.First();
				var lastChart = groupCharts.Last();

				var newQuote = new Quote
				{
					Date = GetGroupingDateTime(firstChart.DateTime, Interval),
					Open = firstChart.Quote.Open,
					High = groupCharts.Max(c => c.Quote.High),
					Low = groupCharts.Min(c => c.Quote.Low),
					Close = lastChart.Quote.Close,
					Volume = groupCharts.Sum(c => c.Quote.Volume)
				};

				newCharts.Add(new ChartInfo(firstChart.Symbol, newQuote));
			}

			Charts = newCharts;
		}

		DateTime GetGroupingDateTime(DateTime dateTime, KlineInterval interval)
		{
			return interval switch
			{
				KlineInterval.ThreeMinutes => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute - (dateTime.Minute % 3), 0),
				KlineInterval.FifteenMinutes => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute - (dateTime.Minute % 15), 0),
				KlineInterval.ThirtyMinutes => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute - (dateTime.Minute % 30), 0),
				KlineInterval.TwoHour => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour - (dateTime.Hour % 2), 0, 0),
				KlineInterval.FourHour => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour - (dateTime.Hour % 4), 0, 0),
				KlineInterval.SixHour => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour - (dateTime.Hour % 6), 0, 0),
				KlineInterval.EightHour => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour - (dateTime.Hour % 8), 0, 0),
				KlineInterval.TwelveHour => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour - (dateTime.Hour % 12), 0, 0),
				KlineInterval.OneWeek => new DateTime(dateTime.AddDays(-((int)dateTime.DayOfWeek + 6) % 7).Year, dateTime.AddDays(-((int)dateTime.DayOfWeek + 6) % 7).Month, dateTime.AddDays(-((int)dateTime.DayOfWeek + 6) % 7).Day, 0, 0, 0),
				KlineInterval.OneMonth => new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0),
				_ => throw new ArgumentException("Invalid interval"),
			};
		}

		string GetGroupingKey(DateTime dateTime, KlineInterval interval)
		{
			return interval switch
			{
				KlineInterval.ThreeMinutes => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour}-{dateTime.Minute / 3}",
				KlineInterval.FifteenMinutes => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour}-{dateTime.Minute / 15}",
				KlineInterval.ThirtyMinutes => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour}-{dateTime.Minute / 30}",
				KlineInterval.TwoHour => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour / 2}",
				KlineInterval.FourHour => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour / 4}",
				KlineInterval.SixHour => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour / 6}",
				KlineInterval.EightHour => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour / 8}",
				KlineInterval.TwelveHour => $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}-{dateTime.Hour / 12}",
				KlineInterval.OneWeek => $"{dateTime.AddDays(-((int)dateTime.DayOfWeek + 6) % 7).Year}-{dateTime.AddDays(-((int)dateTime.DayOfWeek + 6) % 7).Month}-{dateTime.AddDays(-((int)dateTime.DayOfWeek + 6) % 7).Day}",
				KlineInterval.OneMonth => $"{dateTime.Year}-{dateTime.Month}",
				_ => throw new ArgumentException("Invalid interval"),
			};
		}

		public ChartInfo Select()
		{
			return CurrentChart = GetChart(StartTime);
		}

		public ChartInfo Select(DateTime time)
		{
			return CurrentChart = GetChart(time);
		}

		public ChartInfo Next() =>
			CurrentChart == null ?
			CurrentChart = default! :
			CurrentChart = GetChart(Interval switch
			{
				KlineInterval.OneMinute => CurrentChart.DateTime.AddMinutes(1),
				KlineInterval.ThreeMinutes => CurrentChart.DateTime.AddMinutes(3),
				KlineInterval.FiveMinutes => CurrentChart.DateTime.AddMinutes(5),
				KlineInterval.FifteenMinutes => CurrentChart.DateTime.AddMinutes(15),
				KlineInterval.ThirtyMinutes => CurrentChart.DateTime.AddMinutes(30),
				KlineInterval.OneHour => CurrentChart.DateTime.AddHours(1),
				KlineInterval.TwoHour => CurrentChart.DateTime.AddHours(2),
				KlineInterval.FourHour => CurrentChart.DateTime.AddHours(4),
				KlineInterval.SixHour => CurrentChart.DateTime.AddHours(6),
				KlineInterval.EightHour => CurrentChart.DateTime.AddHours(8),
				KlineInterval.TwelveHour => CurrentChart.DateTime.AddHours(12),
				KlineInterval.OneDay => CurrentChart.DateTime.AddDays(1),
				_ => CurrentChart.DateTime.AddMinutes(1)
			});

		public ChartInfo GetChart(DateTime dateTime) => Charts.First(x => x.DateTime.Equals(dateTime));
		public ChartInfo GetLatestChartBefore(DateTime dateTime) => Charts.GetLatestChartBefore(dateTime);
		public List<ChartInfo> GetCharts(DateTime startTime, DateTime endTime)
		{
			return Charts.Where(x => x.DateTime >= startTime && x.DateTime <= endTime).ToList();
		}

		public void CalculateIndicatorsEveryonesCoin()
		{
			var quotes = Charts.Select(x => x.Quote);
			var r1 = quotes.GetLsma(10).Select(x => x.Lsma);
			var r2 = quotes.GetLsma(30).Select(x => x.Lsma);
			var r3 = quotes.GetRsi(14).Select(x => x.Rsi);
			for (int i = 0; i < Charts.Count; i++)
			{
				var chart = Charts[i];
				chart.Lsma1 = r1.ElementAt(i);
				chart.Lsma2 = r2.ElementAt(i);
				chart.Rsi1 = r3.ElementAt(i);
			}
		}

		public void CalculateIndicatorsStefano()
		{
			var quotes = Charts.Select(x => x.Quote);
			var r1 = quotes.GetEma(12).Select(x => x.Ema);
			var r2 = quotes.GetEma(26).Select(x => x.Ema);
			//var r3 = quotes.GetJmaSlope(14).Select(x => x.JmaSlope);
			for (int i = 0; i < Charts.Count; i++)
			{
				var chart = Charts[i];
				chart.Ema1 = r1.ElementAt(i);
				chart.Ema2 = r2.ElementAt(i);
				//chart.JmaSlope = r3.ElementAt(i);
			}
		}

		public void UseRsi(int period1 = 14, int? period2 = null, int? period3 = null)
		{
			var rsi1 = Charts.Select(x => x.Quote).GetRsi(period1).Select(x => x.Rsi);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Rsi1 = rsi1.ElementAt(i);
			}
			if (period2 != null)
			{
				var rsi2 = Charts.Select(x => x.Quote).GetRsi(period2.Value).Select(x => x.Rsi);
				for (int i = 0; i < Charts.Count; i++)
				{
					Charts[i].Rsi2 = rsi2.ElementAt(i);
				}
			}
			if (period3 != null)
			{
				var rsi3 = Charts.Select(x => x.Quote).GetRsi(period3.Value).Select(x => x.Rsi);
				for (int i = 0; i < Charts.Count; i++)
				{
					Charts[i].Rsi3 = rsi3.ElementAt(i);
				}
			}
		}

		public void UseStdev(int period1 = 20)
		{
			var stdev = Charts.Select(x => x.Quote).GetStdev(period1).Select(x => x.Stdev);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Stdev1 = stdev.ElementAt(i);
			}
		}

		public void UseSma(params int[] periods)
		{
			int count = Math.Min(periods.Length, 10);
			for (int p = 0; p < count; p++)
			{
				var sma = Charts.Select(x => x.Quote).GetSma(periods[p]).Select(x => x.Sma);
				for (int i = 0; i < Charts.Count; i++)
				{
					switch (p)
					{
						case 0: Charts[i].Sma1 = sma.ElementAt(i); break;
						case 1: Charts[i].Sma2 = sma.ElementAt(i); break;
						case 2: Charts[i].Sma3 = sma.ElementAt(i); break;
						case 3: Charts[i].Sma4 = sma.ElementAt(i); break;
						case 4: Charts[i].Sma5 = sma.ElementAt(i); break;
						case 5: Charts[i].Sma6 = sma.ElementAt(i); break;
						case 6: Charts[i].Sma7 = sma.ElementAt(i); break;
						case 7: Charts[i].Sma8 = sma.ElementAt(i); break;
						case 8: Charts[i].Sma9 = sma.ElementAt(i); break;
						case 9: Charts[i].Sma10 = sma.ElementAt(i); break;
					}
				}
			}
		}

		public void UseEma(params int[] periods)
		{
			int count = Math.Min(periods.Length, 10);
			for (int p = 0; p < count; p++)
			{
				var ema = Charts.Select(x => x.Quote).GetEma(periods[p]).Select(x => x.Ema);
				for (int i = 0; i < Charts.Count; i++)
				{
					switch (p)
					{
						case 0: Charts[i].Ema1 = ema.ElementAt(i); break;
						case 1: Charts[i].Ema2 = ema.ElementAt(i); break;
						case 2: Charts[i].Ema3 = ema.ElementAt(i); break;
						case 3: Charts[i].Ema4 = ema.ElementAt(i); break;
						case 4: Charts[i].Ema5 = ema.ElementAt(i); break;
						case 5: Charts[i].Ema6 = ema.ElementAt(i); break;
						case 6: Charts[i].Ema7 = ema.ElementAt(i); break;
						case 7: Charts[i].Ema8 = ema.ElementAt(i); break;
						case 8: Charts[i].Ema9 = ema.ElementAt(i); break;
						case 9: Charts[i].Ema10 = ema.ElementAt(i); break;
					}
				}
			}
		}

		public void UseEwmac(int shortPeriod, int longPeriod)
		{
			var ewmac = Charts.Select(x => x.Quote).GetEwmac(shortPeriod, longPeriod).Select(x => x.Ewmac);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Ewmac = ewmac.ElementAt(i);
			}
		}

		public void UseVolatilityRatio(int currentPeriod, int longPeriod)
		{
			var volatilityRatio = Charts.Select(x => x.Quote).GetVolatilityRatio(currentPeriod, longPeriod).Select(x => x.VolatilityRatio);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].VolatilityRatio = volatilityRatio.ElementAt(i);
			}
		}

		public void UseMacd(int fastPeriod1 = 12, int slowPeriod1 = 26, int signalPeriod1 = 9, int? fastPeriod2 = null, int? slowPeriod2 = null, int? signalPeriod2 = null)
		{
			var macd = Charts.Select(x => x.Quote).GetMacd(fastPeriod1, slowPeriod1, signalPeriod1);
			var value = macd.Select(x => x.Macd);
			var signal = macd.Select(x => x.Signal);
			var hist = macd.Select(x => x.Hist);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Macd = value.ElementAt(i);
				Charts[i].MacdSignal = signal.ElementAt(i);
				Charts[i].MacdHist = hist.ElementAt(i);
			}

			if (fastPeriod2 != null)
			{
				var fastPeriod = fastPeriod2 ?? default!;
				var slowPeriod = slowPeriod2 ?? default!;
				var signalPeriod = signalPeriod2 ?? default!;
				var macd2 = Charts.Select(x => x.Quote).GetMacd(fastPeriod, slowPeriod, signalPeriod);
				var value2 = macd.Select(x => x.Macd);
				var signal2 = macd.Select(x => x.Signal);
				var hist2 = macd.Select(x => x.Hist);
				for (int i = 0; i < Charts.Count; i++)
				{
					Charts[i].Macd2 = value2.ElementAt(i);
					Charts[i].MacdSignal2 = signal2.ElementAt(i);
					Charts[i].MacdHist2 = hist2.ElementAt(i);
				}
			}
		}

		public void UseAdx(int adxPeriod = 14, int diPeriod = 14)
		{
			var adx = Charts.Select(x => x.Quote).GetAdx(adxPeriod, diPeriod).Select(x => x.Adx);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Adx = adx.ElementAt(i);
			}
		}

		public void UseSupertrend(int atrPeriod, double factor)
		{
			var supertrend = Charts.Select(x => x.Quote).GetSupertrend(atrPeriod, factor).Select(x => x.Supertrend);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Supertrend1 = supertrend.ElementAt(i);
			}
		}

		public void UseTripleSupertrend(int atrPeriod1, double factor1, int atrPeriod2, double factor2, int atrPeriod3, double factor3)
		{
			var tripleSupertrend = Charts.Select(x => x.Quote).GetTripleSupertrend(atrPeriod1, factor1, atrPeriod2, factor2, atrPeriod3, factor3);
			var supertrend1 = tripleSupertrend.Select(x => x.Supertrend1);
			var supertrend2 = tripleSupertrend.Select(x => x.Supertrend2);
			var supertrend3 = tripleSupertrend.Select(x => x.Supertrend3);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Supertrend1 = supertrend1.ElementAt(i);
				Charts[i].Supertrend2 = supertrend2.ElementAt(i);
				Charts[i].Supertrend3 = supertrend3.ElementAt(i);
			}
		}

		public void UseBollingerBands(int period = 20, double deviation = 2.0)
		{
			var bollingerBands = Charts.Select(x => x.Quote).GetBollingerBands(period, deviation);
			var upper = bollingerBands.Select(x => x.Upper);
			var lower = bollingerBands.Select(x => x.Lower);
			var sma = bollingerBands.Select(x => x.Sma);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Bb1Sma = sma.ElementAt(i);
				Charts[i].Bb1Upper = upper.ElementAt(i);
				Charts[i].Bb1Lower = lower.ElementAt(i);
			}
		}

		public void UseAtr(int period = 14)
		{
			var atr = Charts.Select(x => x.Quote).GetAtr(period).Select(x => x.Atr);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Atr = atr.ElementAt(i);
			}
		}

		public void UseTrendRider(int atrPeriod = 10, double atrMultiplier = 3.0, int rsiPeriod = 14, int macdFastPeriod = 12, int macdSlowPeriod = 26, int macdSignalPeriod = 9)
		{
			var trendRider = Charts.Select(x => x.Quote).GetTrendRider(atrPeriod, atrMultiplier, rsiPeriod, macdFastPeriod, macdSlowPeriod, macdSignalPeriod);
			var trend = trendRider.Select(x => x.Trend);
			var supertrend = trendRider.Select(x => x.Supertrend);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].TrendRiderTrend = trend.ElementAt(i);
				Charts[i].TrendRiderSupertrend = supertrend.ElementAt(i);
			}
		}

		public void UsePredictiveRanges(int period = 200, double factor = 6.0)
		{
			var predictiveRanges = Charts.Select(x => x.Quote).GetPredictiveRanges(period, factor);
			for (int i = 0; i < Charts.Count; i++)
			{
				var pr = predictiveRanges.ElementAt(i);
				Charts[i].PredictiveRangesUpper2 = pr.Upper2;
				Charts[i].PredictiveRangesUpper = pr.Upper;
				Charts[i].PredictiveRangesAverage = pr.Average;
				Charts[i].PredictiveRangesLower = pr.Lower;
				Charts[i].PredictiveRangesLower2 = pr.Lower2;
			}
		}

		public void UseMercuryRanges(int period = 200, double factor = 6.0)
		{
			var mercuryRanges = Charts.Select(x => x.Quote).GetMercuryRanges(period, factor);
			for (int i = 0; i < Charts.Count; i++)
			{
				var mr = mercuryRanges.ElementAt(i);
				Charts[i].MercuryRangesUpper = mr.Upper;
				Charts[i].MercuryRangesAverage = mr.Average;
				Charts[i].MercuryRangesLower = mr.Lower;
			}
		}

		public void UseStoch(int period = 14)
		{
			var stoch = Charts.Select(x => x.Quote).GetStoch(period).Select(x => x.Stoch);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Stoch = stoch.ElementAt(i);
			}
		}

		public void UseCci(int period)
		{
			var cci = Charts.Select(x => x.Quote).GetCci(period).Select(x => x.Cci);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Cci = cci.ElementAt(i);
			}
		}

		public void UseStochasticRsi(int smoothK = 3, int smoothD = 3, int rsiPeriod = 14, int stochasticPeriod = 14)
		{
			var stochasticRsi = Charts.Select(x => x.Quote).GetStochasticRsi(smoothK, smoothD, rsiPeriod, stochasticPeriod);
			var k = stochasticRsi.Select(x => x.K);
			var d = stochasticRsi.Select(x => x.D);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].StochK = k.ElementAt(i);
				Charts[i].StochD = d.ElementAt(i);
			}
		}

		public void UseMlmip(int pivotBars = 20, int momentumWindow = 25, int maxData = 500, int numNeighbors = 100, int predictionSmoothing = 20)
		{
			var mlmip = Charts.Select(x => x.Quote).GetMlmip(pivotBars, momentumWindow, maxData, numNeighbors, predictionSmoothing);
			var prediction = mlmip.Select(x => x.Prediction);
			var predictionMa = mlmip.Select(x => x.PredictionMa);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Prediction = prediction.ElementAt(i);
				Charts[i].PredictionMa = predictionMa.ElementAt(i);
			}
		}

		public void UseAtrma(int atrPeriod = 14, int maPeriod = 20)
		{
			var atrma = Charts.Select(x => x.Quote).GetAtrma(atrPeriod, maPeriod).Select(x => x.Atrma);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Atrma = atrma.ElementAt(i);
			}
		}

		public void UseRsima(int rsiPeriod = 14, int maPeriod = 20)
		{
			var rsima = Charts.Select(x => x.Quote).GetRsima(rsiPeriod, maPeriod).Select(x => x.Rsima);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Rsima = rsima.ElementAt(i);
			}
		}

		public void UseDonchianChannel(int period = 20)
		{
			var donchianChannel = Charts.Select(x => x.Quote).GetDonchianChannel(period);
			var basis = donchianChannel.Select(x => x.Basis);
			var upper = donchianChannel.Select(x => x.Upper);
			var lower = donchianChannel.Select(x => x.Lower);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].DcBasis = basis.ElementAt(i);
				Charts[i].DcUpper = upper.ElementAt(i);
				Charts[i].DcLower = lower.ElementAt(i);
			}
		}

		public void UseSqueezeMomentum(int bbPeriod = 20, double bbFactor = 2.0, int kcPeriod = 20, double kcFactor = 1.5, bool useTrueRange = true)
		{
			var squeezeMomentum = Charts.Select(x => x.Quote).GetSqueezeMomentum(bbPeriod, bbFactor, kcPeriod, kcFactor, useTrueRange);
			var value = squeezeMomentum.Select(x => x.Value);
			var direction = squeezeMomentum.Select(x => x.Direction);
			var signal = squeezeMomentum.Select(x => x.Signal);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].SmValue = value.ElementAt(i);
				Charts[i].SmDirection = direction.ElementAt(i);
				Charts[i].SmSignal = signal.ElementAt(i);
			}
		}

		public void UseCandleScore()
		{
			var candleScore = Charts.Select(x => x.Quote).GetCandleScore().Select(x => x.CandleScore);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].CandleScore = candleScore.ElementAt(i);
			}
		}

		public void UseVwap()
		{
			var vwap = Charts.Select(x => x.Quote).GetVwap().Select(x => x.Vwap);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Vwap = vwap.ElementAt(i);
			}
		}

		public void UseRollingVwap(int period = 20)
		{
			var rollingVwap = Charts.Select(x => x.Quote).GetRollingVwap(period).Select(x => x.RollingVwap);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].RollingVwap = rollingVwap.ElementAt(i);
			}
		}

		public void UseEvwap(int period = 13)
		{
			var evwap = Charts.Select(x => x.Quote).GetEvwap(period).Select(x => x.Evwap);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Evwap = evwap.ElementAt(i);
			}
		}

		public void UseElderRayPower(int emaPeriod = 13)
		{
			var elderRayPower = Charts.Select(x => x.Quote).GetElderRayPower(emaPeriod);
			var bullPower = elderRayPower.Select(x => x.BullPower);
			var bearPower = elderRayPower.Select(x => x.BearPower);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].ElderRayBullPower = bullPower.ElementAt(i);
				Charts[i].ElderRayBearPower = bearPower.ElementAt(i);
			}
		}

		public void UseVolumeSma(int period = 20)
		{
			var volumsSma = Charts.Select(x => x.Quote).GetVolumeSma(period).Select(x => x.Sma);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].VolumeSma = volumsSma.ElementAt(i);
			}
		}
	}
}
