using System.Text.RegularExpressions;

namespace Vectoris.Extensions;

/// <summary>
/// 문자열 및 숫자 관련 확장 메서드
/// </summary>
public static class StringExtensions
{
	#region Signed Number Strings

	/// <summary>
	/// 숫자값을 부호를 포함한 문자열로 변환합니다.
	/// <br/>ex) <c>5.ToSignedString() → "+5"</c>, <c>(-3).ToSignedString() → "-3"</c>
	/// </summary>
	public static string ToSignedString(this int value) => value >= 0 ? $"+{value}" : value.ToString();

	public static string ToSignedString(this double value) => value >= 0 ? $"+{value}" : value.ToString();

	public static string ToSignedString(this decimal value) => value >= 0 ? $"+{value}" : value.ToString();

	/// <summary>
	/// 숫자값을 부호와 퍼센트(%)를 포함한 문자열로 변환합니다.
	/// <br/>ex) <c>5.ToSignedPercentString() → "+5%"</c>, <c>(-3).ToSignedPercentString() → "-3%"</c>
	/// </summary>
	public static string ToSignedPercentString(this int value) => value >= 0 ? $"+{value}%" : $"{value}%";

	public static string ToSignedPercentString(this double value) => value >= 0 ? $"+{value}%" : $"{value}%";

	public static string ToSignedPercentString(this decimal value) => value >= 0 ? $"+{value}%" : $"{value}%";

	#endregion

	#region Split Keep Separators

	/// <summary>
	/// 문자열을 지정한 문자 구분자로 분할하고 구분자를 결과에 포함합니다.
	/// <br/>ex) <c>"a,b;c".SplitKeepSeparators(new[] { ',', ';' }) → ["a", ",", "b", ";", "c"]</c>
	/// </summary>
	/// <param name="str">대상 문자열</param>
	/// <param name="separators">분리할 문자 배열</param>
	/// <returns>구분자를 포함한 문자열 배열</returns>
	public static string[] SplitKeepSeparators(this string? str, char[] separators)
	{
		if (string.IsNullOrEmpty(str)) return Array.Empty<string>();
		string pattern = $@"([{Regex.Escape(new string(separators))}])";
		return Regex.Split(str, pattern);
	}

	/// <summary>
	/// 문자열을 지정한 문자열 구분자로 분할하고 구분자를 결과에 포함합니다.
	/// <br/>ex) <c>"a,==,b".SplitKeepSeparators(new[] { ",==", "," }) → ["a", ",", "==", ",", "b"]</c>
	/// </summary>
	/// <param name="str">대상 문자열</param>
	/// <param name="separators">분리할 문자열 배열</param>
	/// <returns>구분자를 포함한 문자열 배열</returns>
	public static string[] SplitKeepSeparators(this string? str, string[] separators)
	{
		if (string.IsNullOrEmpty(str)) return Array.Empty<string>();

		var escapedSeparators = separators.Select(Regex.Escape);
		string pattern = $"({string.Join('|', escapedSeparators)})";
		return Regex.Split(str, pattern);
	}

	#endregion
}
