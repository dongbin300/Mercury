namespace Vectoris.Charts.Core;

public class Quote(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume = 0)
{
	/// <summary>
	/// 캔들의 시작 시점
	/// </summary>
	public DateTime Time { get; init; } = time;

	public decimal Open { get; init; } = open;
	public decimal High { get; init; } = high;
	public decimal Low { get; init; } = low;
	public decimal Close { get; init; } = close;

	/// <summary>
	/// 거래량
	/// </summary>
	public decimal Volume { get; init; } = volume;
}
