using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 가격 데이터(OHLC) 시계열
/// Series = 시간순으로 정렬된 캔들의 집합
/// </summary>
public class PriceSeries : TimeSeries<Quote>
{
	/// <summary>
	/// 새 가격 시리즈 생성
	/// </summary>
	public PriceSeries() : base() { }

	/// <summary>
	/// 초기 데이터로 가격 시리즈 생성
	/// </summary>
	public PriceSeries(IEnumerable<Quote> quotes) : base()
	{
		foreach (var quote in quotes)
		{
			Add(quote);
		}
	}

	/// <summary>
	/// 캔들 추가 (별칭 메서드)
	/// </summary>
	public void AddQuote(Quote quote) => Add(quote);

	/// <summary>
	/// 여러 캔들 한번에 추가
	/// </summary>
	public void AddQuotes(IEnumerable<Quote> quotes)
	{
		foreach (var quote in quotes)
		{
			Add(quote);
		}
	}

	/// <summary>
	/// 특정 시간의 캔들 조회 (별칭 메서드)
	/// </summary>
	public Quote? GetQuote(DateTime time) => GetByTime(time);

	/// <summary>
	/// 특정 시간 범위의 캔들들 조회 (별칭 메서드)
	/// </summary>
	public IEnumerable<Quote> GetQuotes(DateTime from, DateTime to) => GetRange(from, to);

	/// <summary>
	/// 가장 최근 종가
	/// </summary>
	public decimal? LastClose => Last?.Close;

	/// <summary>
	/// 가장 최근 시가
	/// </summary>
	public decimal? LastOpen => Last?.Open;

	/// <summary>
	/// 가장 최근 고가
	/// </summary>
	public decimal? LastHigh => Last?.High;

	/// <summary>
	/// 가장 최근 저가
	/// </summary>
	public decimal? LastLow => Last?.Low;

	/// <summary>
	/// N개 최근 캔들 조회
	/// </summary>
	public IReadOnlyList<Quote> GetLastQuotes(int count)
	{
		if (count <= 0 || _values.Count == 0)
			return [];

		var startIndex = Math.Max(0, _values.Count - count);
		return _values[startIndex..].ToList();
	}

	/// <summary>
	/// N개 이전 캔들 조회 (현재 캔들 제외)
	/// </summary>
	public IReadOnlyList<Quote> GetPreviousQuotes(int count)
	{
		if (count <= 0 || _values.Count <= 1)
			return [];

		var startIndex = Math.Max(0, _values.Count - count - 1);
		var endIndex = _values.Count - 1;
		return _values[startIndex..endIndex].ToList();
	}
}

