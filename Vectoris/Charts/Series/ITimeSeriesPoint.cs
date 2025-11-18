namespace Vectoris.Charts.Series;

/// <summary>
/// 시간 기반 시계열 데이터 인터페이스
/// </summary>
public interface ITimeSeriesPoint
{
	DateTime Time { get; }

	public string ToString() =>
		$"{Time:yyyy-MM-dd HH:mm:ss}";
}
