using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 하위 호환성을 위한 LineSeries (향후 제거 권장)
/// </summary>
[Obsolete("Use ValueSeries instead. This class will be removed in future versions.")]
public class LineSeries(string name) : Series<IndicatorValue>
{
	public string Name { get; } = name;

	public IndicatorValue? GetValue(DateTime time) =>
		GetByTime(time);
}
