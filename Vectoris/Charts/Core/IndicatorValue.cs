using Vectoris.Charts.Series;

namespace Vectoris.Charts.Core;

public class IndicatorValue(DateTime time, decimal? value) : ITimeSeriesPoint
{
	public DateTime Time { get; init; } = time;
	public decimal? Value { get; init; } = value;

	public override string ToString()
	{
		return Value.HasValue
			? $"{Time:yyyy-MM-dd HH:mm:ss} | {Value.Value}"
			: $"{Time:yyyy-MM-dd HH:mm:ss} | -";
	}
}
