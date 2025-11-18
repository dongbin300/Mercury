namespace Vectoris.Charts.Core;

/// <summary>
/// 모든 지표의 공통 인터페이스
/// 배치/실시간 계산 공용
/// </summary>
public interface IIndicator
{
	/// <summary>
	/// 지표 이름
	/// </summary>
	string Name { get; }
	/// <summary>
	/// 현재 계산된 최신 값
	/// </summary>
	decimal? Current { get; }
	/// <summary>
	/// 전체 계산 값 (배치/실시간 누적)
	/// </summary>
	IReadOnlyList<decimal?> Values { get; }
	/// <summary>
	/// 단일 캔들 추가 (실시간 계산)
	/// </summary>
	void AddQuote(Quote quote);
	/// <summary>
	/// 배치용 캔들 추가
	/// </summary>
	void AddQuotes(IEnumerable<Quote> quotes);
}
