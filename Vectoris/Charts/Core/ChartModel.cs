using Vectoris.Charts.Series;

namespace Vectoris.Charts.Core;

/// <summary>
/// 차트 모델
/// CandleSeries + 여러 IndicatorSeries를 관리
/// </summary>
public class ChartModel
{
	/// <summary>
	/// 캔들 시계열
	/// </summary>
	public CandleSeries CandleSeries { get; } = new();

	/// <summary>
	/// 모든 지표(LineSeries) 집합
	/// </summary>
	public IndicatorSeries IndicatorSeries { get; } = new();

	/// <summary>
	/// 차트에 새로운 캔들 추가
	/// </summary>
	public void AddQuote(Quote quote)
	{
		CandleSeries.AddQuote(quote);

		// 필요 시 각 LineSeries에 계산 후 자동 추가
		foreach (var line in IndicatorSeries.Lines)
		{
			// 예: 계산기에서 최신 quote 기준 값 계산 후 추가
			// line.AddValue(new IndicatorValue(quote.Time, value));
		}
	}

	/// <summary>
	/// LineSeries 추가
	/// </summary>
	public void AddLineSeries(LineSeries line)
	{
		IndicatorSeries.AddLine(line);
	}

	/// <summary>
	/// 특정 시간 범위에 대한 차트 데이터 조회
	/// </summary>
	public IEnumerable<Quote> GetQuotes(DateTime from, DateTime to) =>
		CandleSeries.GetRange(from, to);

	/// <summary>
	/// 특정 LineSeries 값 조회
	/// </summary>
	public LineSeries? GetLineSeries(string name) =>
		IndicatorSeries.GetLine(name);

	/// <summary>
	/// 특정 시간에 대한 캔들 + 지표값 패키지
	/// </summary>
	public ChartPoint GetChartPoint(DateTime time)
	{
		var quote = CandleSeries.GetQuote(time) ?? throw new ArgumentException($"No quote at {time}");
		var indicators = IndicatorSeries.Lines
			.Select(line => new KeyValuePair<string, double?>(line.Name, line.GetValue(time)?.Value))
			.ToDictionary(kv => kv.Key, kv => kv.Value);

		return new ChartPoint(quote, indicators);
	}
}
