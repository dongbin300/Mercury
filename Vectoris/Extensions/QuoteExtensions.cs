using Binance.Net.Enums;

using Vectoris.Charts.Core;

namespace Vectoris.Extensions;

public static class QuoteExtensions
{
	/// <summary>
	/// 기본 간격(1분, 5분, 1시간, 1일)이 아닌 경우 더 높은 간격으로 캔들을 변환
	/// </summary>
	/// <param name="quotes">원본 Quote 리스트</param>
	/// <param name="interval">목표 KlineInterval</param>
	/// <returns>변환된 Quote 리스트</returns>
	public static List<Quote> Aggregate(this List<Quote> quotes, KlineInterval interval)
	{
		var newQuotes = new List<Quote>();
		var groupedByInterval = quotes.GroupBy(q => GetGroupingKey(q.Time, interval));

		foreach (var group in groupedByInterval)
		{
			var groupQuotes = group.ToList();
			var firstQuote = groupQuotes.First();
			var lastQuote = groupQuotes.Last();

			var newQuote = new Quote(
				GetGroupingDateTime(firstQuote.Time, interval),
				firstQuote.Open,
				groupQuotes.Max(q => q.High),
				groupQuotes.Min(q => q.Low),
				lastQuote.Close,
				groupQuotes.Sum(q => q.Volume)
			);

			newQuotes.Add(newQuote);
		}

		return newQuotes;
	}

	private static DateTime GetGroupingDateTime(DateTime dateTime, KlineInterval interval)
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

	private static string GetGroupingKey(DateTime dateTime, KlineInterval interval)
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
}
