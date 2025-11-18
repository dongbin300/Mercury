using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 단일 값 시계열 (지표, 데이터 등)
/// Series = 시간순으로 정렬된 값들의 집합
/// </summary>
public class ValueSeries : TimeSeries<IndicatorValue>
{
	/// <summary>
	/// 시리즈 이름
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 새 값 시리즈 생성
	/// </summary>
	public ValueSeries(string name) : base()
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	/// <summary>
	/// 초기 데이터로 값 시리즈 생성
	/// </summary>
	public ValueSeries(string name, IEnumerable<IndicatorValue> values) : base()
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		foreach (var value in values)
		{
			Add(value);
		}
	}

	/// <summary>
	/// 값 추가 (별칭 메서드)
	/// </summary>
	public void AddValue(IndicatorValue value) => Add(value);

	/// <summary>
	/// 값 추가 (간편 메서드)
	/// </summary>
	public void AddValue(DateTime time, decimal? value) => Add(new IndicatorValue(time, value));

	/// <summary>
	/// 여러 값 한번에 추가
	/// </summary>
	public void AddValues(IEnumerable<IndicatorValue> values)
	{
		foreach (var value in values)
		{
			Add(value);
		}
	}

	/// <summary>
	/// 특정 시간의 값 조회 (별칭 메서드)
	/// </summary>
	public IndicatorValue? GetValue(DateTime time) => GetByTime(time);

	/// <summary>
	/// 가장 최근 값
	/// </summary>
	public decimal? LastValue => Last?.Value;

	/// <summary>
	/// N개 최근 값 조회
	/// </summary>
	public IReadOnlyList<IndicatorValue> GetLastValues(int count)
	{
		if (count <= 0 || _values.Count == 0)
			return [];

		var startIndex = Math.Max(0, _values.Count - count);
		return _values[startIndex..].ToList();
	}

	/// <summary>
	/// 특정 시간 이전의 마지막 값 조회
	/// </summary>
	public IndicatorValue? GetLastValueBefore(DateTime time)
	{
		return _values.LastOrDefault(v => v.Time < time);
	}

	/// <summary>
	/// 값들의 단순 이동 평균 계산
	/// </summary>
	public decimal? CalculateSimpleMovingAverage(int period)
	{
		if (period <= 0 || _values.Count < period)
			return null;

		var lastValues = GetLastValues(period);
		if (!lastValues.Any())
			return null;

		return lastValues.Average(v => v.Value);
	}

	/// <summary>
	/// 특정 시간 범위의 평균값 계산
	/// </summary>
	public decimal? CalculateAverage(DateTime from, DateTime to)
	{
		var values = GetRange(from, to).Where(v => v.Value.HasValue);
		if (!values.Any())
			return null;

		return values.Average(v => v.Value!.Value);
	}
}

