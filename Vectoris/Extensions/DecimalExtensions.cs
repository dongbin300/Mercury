namespace Vectoris.Extensions;

/// <summary>
/// 숫자형 및 문자열/객체 변환 관련 확장 메서드
/// </summary>
public static class DecimalExtensions
{
	#region Array Extensions

	/// <summary>
	/// double 배열을 nullable double 배열로 변환합니다.
	/// <br/>ex) <c>new double[] {1.0, 2.0}.ToNullable() → [1.0, 2.0]</c>
	/// </summary>
	public static double?[] ToNullable(this double[] source) =>
		source.Select(item => (double?)item).ToArray();

	#endregion

	#region Rounding

	/// <summary>
	/// double 값을 지정한 자리수로 반올림합니다.
	/// <br/>ex) <c>3.14159.Round(2) → 3.14</c>
	/// </summary>
	public static double Round(this double value, int digits) => Math.Round(value, digits);

	/// <summary>
	/// decimal 값을 지정한 자리수로 반올림합니다.
	/// <br/>ex) <c>3.14159m.Round(2) → 3.14m</c>
	/// </summary>
	public static decimal Round(this decimal value, int digits) => Math.Round(value, digits);

	#endregion

	#region String / Object Conversion

	/// <summary>
	/// 문자열을 int로 변환합니다. 변환 실패 시 0 반환.
	/// <br/>ex) <c>"123".ToInt() → 123</c>, <c>"abc".ToInt() → 0</c>
	/// </summary>
	public static int ToInt(this string? value) =>
		int.TryParse(value, out var result) ? result : 0;

	/// <summary>
	/// 객체를 int로 변환합니다. 변환 실패 시 0 반환.
	/// <br/>ex) <c>123.ToInt() → 123</c>, <c>"abc".ToInt() → 0</c>
	/// </summary>
	public static int ToInt(this object? value)
	{
		if (value is null) return 0;
		if (value is int i) return i;
		return int.TryParse(value.ToString(), out var result) ? result : 0;
	}

	/// <summary>
	/// 문자열을 double로 변환합니다. 변환 실패 시 0 반환.
	/// </summary>
	public static double ToDouble(this string? value) =>
		double.TryParse(value, out var result) ? result : 0;

	/// <summary>
	/// 문자열을 decimal로 변환합니다. 변환 실패 시 0 반환.
	/// </summary>
	public static decimal ToDecimal(this string? value) =>
		decimal.TryParse(value, out var result) ? result : 0m;

	/// <summary>
	/// 객체를 decimal로 변환합니다. 변환 실패 시 0 반환.
	/// </summary>
	public static decimal ToDecimal(this object? value)
	{
		if (value is null) return 0m;
		if (value is decimal d) return d;
		return decimal.TryParse(value.ToString(), out var result) ? result : 0m;
	}

	/// <summary>
	/// 문자열을 long으로 변환합니다. 변환 실패 시 0 반환.
	/// </summary>
	public static long ToLong(this string? value) =>
		long.TryParse(value, out var result) ? result : 0L;

	#endregion
}
