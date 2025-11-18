using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 하위 호환성을 위한 CandleSeries (향후 제거 권장)
/// </summary>
[Obsolete("Use PriceSeries instead. This class will be removed in future versions.")]
public class CandleSeries : Series<Quote>
{
	public CandleSeries() { }
}
