namespace Vectoris.Charts.Core;

/// <summary>
/// 특정 시점 캔들 + 지표값 패키지
/// </summary>
public class ChartPoint(Quote quote, IReadOnlyDictionary<string, double?> indicatorValues)
{
	public Quote Quote { get; } = quote;
	public IReadOnlyDictionary<string, double?> IndicatorValues { get; } = indicatorValues;
}
