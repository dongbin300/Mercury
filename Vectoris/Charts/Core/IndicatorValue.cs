namespace Vectoris.Charts.Core;

/// <summary>
/// 단일 시점의 지표 계산 결과
/// </summary>
/// <param name="time">계산 시점</param>
/// <param name="value">계산 값</param>
/// <param name="indicatorName">지표 이름</param>
public class IndicatorValue(DateTime time, double? value, string indicatorName)
{
	/// <summary>
	/// 계산 대상 시점 (캔들/Quote 시간)
	/// </summary>
	public DateTime Time { get; init; } = time;

	/// <summary>
	/// 계산된 값
	/// </summary>
	public double? Value { get; init; } = value;

	/// <summary>
	/// 지표 이름 (예: EMA20, RSI14)
	/// </summary>
	public string IndicatorName { get; init; } = indicatorName;

	/// <summary>
	/// 값 존재 여부
	/// </summary>
	public bool HasValue => Value.HasValue;

	/// <summary>
	/// 값이 없으면 기본값 제공
	/// </summary>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public double GetValueOrDefault(double defaultValue = 0)
	{
		return Value ?? defaultValue;
	}

	public override string ToString()
	{
		return $"{Time:yyyy-MM-dd HH:mm:ss} | {IndicatorName} = {Value?.ToString("F4") ?? "null"}";
	}
}
