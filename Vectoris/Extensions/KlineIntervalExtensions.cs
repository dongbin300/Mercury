using Binance.Net.Enums;

namespace Vectoris.Extensions;

/// <summary>
/// KlineInterval 관련 변환 및 유틸 확장 메서드
/// <br/>문자열 ↔ KlineInterval, TimeSpan, 표준 문자열 변환 지원
/// </summary>
public static class KlineIntervalExtensions
{
	/// <summary>
	/// 문자열을 KlineInterval Enum으로 변환합니다.
	/// <br/>ex) <c>"1m".ToKlineInterval() → KlineInterval.OneMinute</c>
	/// </summary>
	/// <param name="intervalString">Binance 스타일 Kline 문자열 (ex: "1m", "1D")</param>
	/// <returns>KlineInterval Enum</returns>
	public static KlineInterval ToKlineInterval(this string intervalString) =>
		intervalString switch
		{
			"1m" => KlineInterval.OneMinute,
			"3m" => KlineInterval.ThreeMinutes,
			"5m" => KlineInterval.FiveMinutes,
			"15m" => KlineInterval.FifteenMinutes,
			"30m" => KlineInterval.ThirtyMinutes,
			"1h" => KlineInterval.OneHour,
			"2h" => KlineInterval.TwoHour,
			"4h" => KlineInterval.FourHour,
			"6h" => KlineInterval.SixHour,
			"8h" => KlineInterval.EightHour,
			"12h" => KlineInterval.TwelveHour,
			"1D" => KlineInterval.OneDay,
			"3D" => KlineInterval.ThreeDay,
			"1W" => KlineInterval.OneWeek,
			"1M" => KlineInterval.OneMonth,
			_ => KlineInterval.OneMinute
		};

	/// <summary>
	/// KlineInterval을 Binance 스타일 문자열로 변환합니다.
	/// <br/>ex) <c>KlineInterval.OneMinute.ToKlineIntervalString() → "1m"</c>
	/// </summary>
	public static string ToKlineIntervalString(this KlineInterval interval) =>
		interval switch
		{
			KlineInterval.OneMinute => "1m",
			KlineInterval.ThreeMinutes => "3m",
			KlineInterval.FiveMinutes => "5m",
			KlineInterval.FifteenMinutes => "15m",
			KlineInterval.ThirtyMinutes => "30m",
			KlineInterval.OneHour => "1h",
			KlineInterval.TwoHour => "2h",
			KlineInterval.FourHour => "4h",
			KlineInterval.SixHour => "6h",
			KlineInterval.EightHour => "8h",
			KlineInterval.TwelveHour => "12h",
			KlineInterval.OneDay => "1D",
			KlineInterval.ThreeDay => "3D",
			KlineInterval.OneWeek => "1W",
			KlineInterval.OneMonth => "1M",
			_ => "1m"
		};

	/// <summary>
	/// KlineInterval을 TimeSpan으로 변환합니다.
	/// <br/>ex) <c>KlineInterval.OneHour.ToKlineTimeSpan() → TimeSpan.FromHours(1)</c>
	/// </summary>
	public static TimeSpan ToKlineTimeSpan(this KlineInterval interval) =>
		interval switch
		{
			KlineInterval.OneMinute => TimeSpan.FromMinutes(1),
			KlineInterval.ThreeMinutes => TimeSpan.FromMinutes(3),
			KlineInterval.FiveMinutes => TimeSpan.FromMinutes(5),
			KlineInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
			KlineInterval.ThirtyMinutes => TimeSpan.FromMinutes(30),
			KlineInterval.OneHour => TimeSpan.FromHours(1),
			KlineInterval.TwoHour => TimeSpan.FromHours(2),
			KlineInterval.FourHour => TimeSpan.FromHours(4),
			KlineInterval.SixHour => TimeSpan.FromHours(6),
			KlineInterval.EightHour => TimeSpan.FromHours(8),
			KlineInterval.TwelveHour => TimeSpan.FromHours(12),
			KlineInterval.OneDay => TimeSpan.FromDays(1),
			KlineInterval.ThreeDay => TimeSpan.FromDays(3),
			KlineInterval.OneWeek => TimeSpan.FromDays(7),
			KlineInterval.OneMonth => TimeSpan.FromDays(30),
			_ => TimeSpan.FromMinutes(1)
		};

	/// <summary>
	/// KlineInterval을 표준 단위 문자열로 변환합니다 (보고/기록용 단위).
	/// <br/>ex) <c>KlineInterval.FiveMinutes.ToStandardString() → "5m"</c>
	/// </summary>
	public static string ToStandardString(this KlineInterval interval) =>
		interval switch
		{
			KlineInterval.OneMinute => "1m",
			KlineInterval.ThreeMinutes => "1m",
			KlineInterval.FiveMinutes => "5m",
			KlineInterval.FifteenMinutes => "5m",
			KlineInterval.ThirtyMinutes => "5m",
			KlineInterval.OneHour => "1h",
			KlineInterval.TwoHour => "1h",
			KlineInterval.FourHour => "1h",
			KlineInterval.SixHour => "1h",
			KlineInterval.EightHour => "1h",
			KlineInterval.TwelveHour => "1h",
			KlineInterval.OneDay => "1D",
			KlineInterval.ThreeDay => "1D",
			KlineInterval.OneWeek => "1D",
			KlineInterval.OneMonth => "1D",
			_ => "1m"
		};
}
