namespace Vectoris.Charts.Series;

/// <summary>
/// 시계열 데이터의 기본 구현 클래스
/// Series = 시간순으로 정렬된 데이터 포인트들의 집합 (캔들, 지표값 등)
/// </summary>
public class TimeSeries<T> : ITimeSeries<T> where T : class, ITimeSeriesPoint
{
	protected readonly List<T> _values = [];

	/// <summary>
	/// 시계열 데이터 포인트들을 읽기 전용으로 조회
	/// </summary>
	public IReadOnlyList<T> Values => _values;

	/// <summary>
	/// 시계열의 데이터 포인트 수
	/// </summary>
	public int Count => _values.Count;

	/// <summary>
	/// 가장 최근 데이터 포인트
	/// </summary>
	public T? Last => _values.Count > 0 ? _values[^1] : null;

	/// <summary>
	/// 가장 오래된 데이터 포인트
	/// </summary>
	public T? First => _values.Count > 0 ? _values[0] : null;

	/// <summary>
	/// 시계열에 새로운 데이터 포인트 추가
	/// </summary>
	public virtual void Add(T value)
	{
		if (_values.Count > 0)
		{
			var last = _values[^1];
			if (value.Time <= last.Time)
				throw new ArgumentException("New value time must be greater than the last value.");
		}

		_values.Add(value);
	}

	/// <summary>
	/// 특정 시간의 데이터 포인트 조회
	/// </summary>
	public virtual T? GetByTime(DateTime time) =>
		_values.FirstOrDefault(v => v.Time == time);

	/// <summary>
	/// 특정 시간 범위의 데이터 포인트들 조회
	/// </summary>
	public virtual IEnumerable<T> GetRange(DateTime from, DateTime to) =>
		_values.Where(v => v.Time >= from && v.Time <= to);

	/// <summary>
	/// 인덱스로 데이터 포인트 조회
	/// </summary>
	public T? GetByIndex(int index)
	{
		return index >= 0 && index < _values.Count ? _values[index] : null;
	}

	/// <summary>
	/// 시계열 데이터 모두 제거
	/// </summary>
	public void Clear()
	{
		_values.Clear();
	}
}

/// <summary>
/// 하위 호환성을 위한 추상 클래스 (향후 제거 권장)
/// </summary>
[Obsolete("Use TimeSeries<T> instead. This class will be removed in future versions.")]
public abstract class Series<T> where T : class, ITimeSeriesPoint
{
	protected readonly List<T> _values = [];

	public IReadOnlyList<T> Values =>
		_values;

	public void Add(T value)
	{
		if (_values.Count > 0)
		{
			var last = _values[^1];
			if (value.Time <= last.Time)
				throw new ArgumentException("New value time must be greater than the last value.");
		}

		_values.Add(value);
	}

	public T? GetByTime(DateTime time) =>
		_values.FirstOrDefault(v => v.Time == time);

	public IEnumerable<T> GetRange(DateTime from, DateTime to) =>
		_values.Where(v => v.Time >= from && v.Time <= to);
}
