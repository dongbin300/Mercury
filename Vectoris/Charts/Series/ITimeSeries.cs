namespace Vectoris.Charts.Series;

/// <summary>
/// 시계열 컬렉션의 기본 동작을 정의하는 인터페이스
/// </summary>
/// <typeparam name="T">시계열 데이터 타입 (ITimeSeriesPoint를 구현해야 함)</typeparam>
public interface ITimeSeries<T> where T : class, ITimeSeriesPoint
{
	/// <summary>
	/// 시계열 데이터 포인트들을 읽기 전용으로 조회
	/// </summary>
	IReadOnlyList<T> Values { get; }

	/// <summary>
	/// 시계열에 새로운 데이터 포인트 추가
	/// </summary>
	void Add(T value);

	/// <summary>
	/// 특정 시간의 데이터 포인트 조회
	/// </summary>
	T? GetByTime(DateTime time);

	/// <summary>
	/// 특정 시간 범위의 데이터 포인트들 조회
	/// </summary>
	IEnumerable<T> GetRange(DateTime from, DateTime to);

	/// <summary>
	/// 시계열의 데이터 포인트 수
	/// </summary>
	int Count { get; }

	/// <summary>
	/// 가장 최근 데이터 포인트
	/// </summary>
	T? Last { get; }

	/// <summary>
	/// 가장 오래된 데이터 포인트
	/// </summary>
	T? First { get; }
}