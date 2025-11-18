using Vectoris.Charts.Series;
using Vectoris.Enums;

namespace Vectoris.Charts.Core;

/// <summary>
/// 차트 모델
/// PriceSeries + 여러 ValueSeries를 관리
/// </summary>
public class ChartModel
{
	/// <summary>
	/// 가격 시계열 (캔들 데이터)
	/// </summary>
	public PriceSeries PriceSeries { get; } = new();

	/// <summary>
	/// 모든 지표 시리즈 컬렉션
	/// </summary>
	public SeriesCollection Indicators { get; } = new();

	/// <summary>
	/// 새 차트 모델 생성
	/// </summary>
	public ChartModel() { }

	/// <summary>
	/// 초기 가격 데이터로 차트 모델 생성
	/// </summary>
	public ChartModel(IEnumerable<Quote> quotes)
	{
		PriceSeries.AddQuotes(quotes);
	}

	/// <summary>
	/// 차트에 새로운 캔들 추가
	/// </summary>
	public void AddQuote(Quote quote)
	{
		PriceSeries.AddQuote(quote);

		// 필요 시 각 지표 시리즈에 계산 후 자동 추가
		// 예: 지표 계산기에서 최신 quote 기준 값 계산 후 추가
		// foreach (var indicator in Indicators.Series)
		// {
		//     var value = CalculateIndicator(indicator.Name, quote);
		//     indicator.AddValue(quote.Time, value);
		// }
	}

	/// <summary>
	/// 여러 캔들 한번에 추가
	/// </summary>
	public void AddQuotes(IEnumerable<Quote> quotes)
	{
		PriceSeries.AddQuotes(quotes);
	}

	/// <summary>
	/// 지표 시리즈 추가
	/// </summary>
	public void AddIndicator(ValueSeries indicator)
	{
		Indicators.Add(indicator);
	}

	/// <summary>
	/// 이름으로 지표 시리즈 생성 및 추가
	/// </summary>
	public ValueSeries CreateIndicator(string name)
	{
		return Indicators.CreateSeries(name);
	}

	/// <summary>
	/// 특정 시간 범위에 대한 가격 데이터 조회
	/// </summary>
	public IEnumerable<Quote> GetQuotes(DateTime from, DateTime to) =>
		PriceSeries.GetRange(from, to);

	/// <summary>
	/// 특정 지표 시리즈 조회
	/// </summary>
	public ValueSeries? GetIndicator(string name) =>
		Indicators.GetSeries(name);

	/// <summary>
	/// 특정 시간에 대한 캔들 + 지표값 패키지
	/// </summary>
	public ChartPoint GetChartPoint(DateTime time)
	{
		var quote = PriceSeries.GetQuote(time) ?? throw new ArgumentException($"No quote at {time}");
		var indicatorValues = Indicators.GetAllValuesAt(time);

		// decimal?을 double?로 변환
		var indicators = indicatorValues
			.ToDictionary(kv => kv.Key, kv => kv.Value.HasValue ? (double?)kv.Value.Value : null);

		return new ChartPoint(quote, indicators);
	}

	/// <summary>
	/// 특정 시간 범위에 대한 차트 데이터 조회
	/// </summary>
	public IEnumerable<ChartPoint> GetChartPoints(DateTime from, DateTime to)
	{
		var quotes = PriceSeries.GetRange(from, to);
		var indicatorRanges = Indicators.GetRangeValues(from, to);

		foreach (var quote in quotes)
		{
			var indicators = new Dictionary<string, double?>();
			foreach (var kvp in indicatorRanges)
			{
				var value = kvp.Value.FirstOrDefault(v => v.Time == quote.Time);
				indicators[kvp.Key] = value?.Value.HasValue == true ? (double?)value.Value.Value : null;
			}

			yield return new ChartPoint(quote, indicators);
		}
	}

	/// <summary>
	/// 모든 데이터 정리
	/// </summary>
	public void Clear()
	{
		PriceSeries.Clear();
		Indicators.Clear();
	}

	/// <summary>
	/// 가격 데이터 수
	/// </summary>
	public int PriceCount => PriceSeries.Count;

	/// <summary>
	/// 지표 시리즈 수
	/// </summary>
	public int IndicatorCount => Indicators.Count;

	/// <summary>
	/// 지표 시리즈 가져오기
	/// 이미 계산되어 있으면 반환, 없으면 생성 후 계산
	/// </summary>
	/// <param name="type">지표 이름 (예: "EMA")</param>
	/// <param name="args">파라미터 (예: 20)</param>
	/// 
	public ValueSeries Get(string type, params object[] args)
	{
		if (string.IsNullOrWhiteSpace(type))
			throw new ArgumentNullException(nameof(type));

		string key = $"{type}_{string.Join("_", args.Select(a => a.ToString() ?? "null"))}";

		// 이미 존재하는 시리즈면 반환
		var existing = Indicators.GetSeries(key);
		if (existing != null)
			return existing;

		var series = Indicators.CreateSeries(key);

		IndicatorBase indicator = IndicatorFactory.Get(type, args);

		foreach (var quote in PriceSeries.GetRange(DateTime.MinValue, DateTime.MaxValue))
		{
			indicator.AddQuote(quote);
			series.AddValue(quote.Time, indicator.Current);
		}

		return series;
	}

}
