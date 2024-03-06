using Binance.Net.Enums;

using Microsoft.VisualBasic;

namespace Mercury.Charts
{
	public class ChartPack
	{
		public string Symbol => Charts.First().Symbol;
		public KlineInterval Interval = KlineInterval.OneMinute;
		public IList<ChartInfo> Charts { get; set; } = new List<ChartInfo>();
		public DateTime StartTime => Charts.Min(x => x.DateTime);
		public DateTime EndTime => Charts.Max(x => x.DateTime);
		public ChartInfo? CurrentChart;

		public ChartPack(KlineInterval interval)
		{
			Interval = interval;
			CurrentChart = null;
		}

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

		public void UseRsi(int period = 14)
		{
			var rsi = Charts.Select(x => x.Quote).GetRsi(period).Select(x => x.Rsi);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Rsi1 = rsi.ElementAt(i);
			}
		}

		public void UseEma(int period)
		{
			var ema = Charts.Select(x => x.Quote).GetEma(period).Select(x => x.Ema);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Ema1 = ema.ElementAt(i);
			}
		}

		public void UseMacd(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
		{
			var macd = Charts.Select(x => x.Quote).GetMacd(fastPeriod, slowPeriod, signalPeriod);
			var value = macd.Select(x => x.Macd);
			var signal = macd.Select(x => x.Signal);
			var hist = macd.Select(x => x.Hist);
			for (int i = 0; i < Charts.Count; i++)
			{
				Charts[i].Macd = value.ElementAt(i);
				Charts[i].MacdSignal = signal.ElementAt(i);
				Charts[i].MacdHist = hist.ElementAt(i);
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
	}
}
