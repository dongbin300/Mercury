using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 단일 지표 선 시리즈
/// </summary>
public class LineSeries(string name)
{
	private readonly List<IndicatorValue> _values = [];

	public string Name { get; init; } = name;

	public IReadOnlyList<IndicatorValue> Values =>
		_values;

	public void AddValue(IndicatorValue value)
	{
		if (_values.Count > 0 && value.Time <= _values.Last().Time)
			throw new ArgumentException("New value time must be greater than the last value.");

		_values.Add(value);
	}

	public IndicatorValue? GetValue(DateTime time) =>
		_values.FirstOrDefault(v => v.Time == time);
}
