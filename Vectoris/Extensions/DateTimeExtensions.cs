namespace Vectoris.Extensions;

public static class DateTimeExtensions
{
	/// <summary>
	/// 문자열을 DateTime으로 변환합니다.
	/// <br/>ex) <c>"2024-01-01 12:00:00".ParseToDateTime()</c>
	/// </summary>
	/// <remarks>
	/// 포맷이 명확할 경우 <c>DateTime.ParseExact</c> 사용을 권장합니다.
	/// </remarks>
	public static DateTime ToDateTime(this string value)
		=> DateTime.Parse(value);

	/// <summary>
	/// Unix Timestamp(밀리초)를 UTC DateTime으로 변환합니다.
	/// <br/>ex) <c>1700000000000L.ToUtcDateTime()</c>
	/// </summary>
	public static DateTime ToUtcDateTime(this long timestampMilliseconds)
		=> DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds).UtcDateTime;

	/// <summary>
	/// yyyy-MM-dd HH:mm:ss 포맷의 문자열을 반환합니다.
	/// <br/>ex) <c>DateTime.Now.ToStandardString()</c>
	/// </summary>
	public static string ToStandardString(this DateTime dateTime)
		=> dateTime.ToString("yyyy-MM-dd HH:mm:ss");

	/// <summary>
	/// yyyy_MM_dd_HH_mm_ss 포맷의 파일명용 문자열을 반환합니다.
	/// <br/>ex) <c>DateTime.Now.ToStandardFileName()</c>
	/// </summary>
	public static string ToStandardFileName(this DateTime dateTime)
		=> dateTime.ToString("yyyy_MM_dd_HH_mm_ss");

	/// <summary>
	/// yyyyMMddHHmmss 포맷의 파일명용 단축 문자열을 반환합니다.
	/// <br/>ex) <c>DateTime.Now.ToSimpleFileName()</c>
	/// </summary>
	public static string ToSimpleFileName(this DateTime dateTime)
		=> dateTime.ToString("yyyyMMddHHmmss");

	/// <summary>
	/// DateTime을 Unix Timestamp(초 단위)로 변환합니다.
	/// <br/>ex) <c>DateTime.UtcNow.ToUnixSeconds()</c>
	/// </summary>
	public static long ToUnixSeconds(this DateTime value)
		=> ((DateTimeOffset)value).ToUnixTimeSeconds();

	/// <summary>
	/// DateTime을 Unix Timestamp(밀리초 단위)로 변환합니다.
	/// <br/>ex) <c>DateTime.UtcNow.ToUnixMilliseconds()</c>
	/// </summary>
	public static long ToUnixMilliseconds(this DateTime value)
		=> ((DateTimeOffset)value).ToUnixTimeMilliseconds();

	/// <summary>
	/// Unix Timestamp(초 단위)를 UTC DateTime으로 변환합니다.
	/// <br/>ex) <c>1700000000L.UnixSecondsToUtc()</c>
	/// </summary>
	public static DateTime UnixSecondsToUtc(this long value)
		=> DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;

	/// <summary>
	/// Unix Timestamp(밀리초 단위)를 UTC DateTime으로 변환합니다.
	/// <br/>ex) <c>1700000000000L.UnixMillisecondsToUtc()</c>
	/// </summary>
	public static DateTime UnixMillisecondsToUtc(this long value)
		=> DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
}
