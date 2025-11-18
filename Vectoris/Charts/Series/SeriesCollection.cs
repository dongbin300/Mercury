using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 여러 ValueSeries를 관리하는 컬렉션
/// 여러 지표 시리즈들을 그룹화하여 관리
/// </summary>
public class SeriesCollection
{
	private readonly Dictionary<string, ValueSeries> _series = new();

	/// <summary>
	/// 관리 중인 모든 시리즈 조회
	/// </summary>
	public IReadOnlyCollection<ValueSeries> Series => _series.Values;

	/// <summary>
	/// 관리 중인 모든 시리즈 이름 조회
	/// </summary>
	public IReadOnlyCollection<string> Names => _series.Keys;

	/// <summary>
	/// 시리즈 수
	/// </summary>
	public int Count => _series.Count;

	/// <summary>
	/// 새 시리즈 컬렉션 생성
	/// </summary>
	public SeriesCollection() { }

	/// <summary>
	/// 초기 시리즈들로 컬렉션 생성
	/// </summary>
	public SeriesCollection(IEnumerable<ValueSeries> series)
	{
		foreach (var s in series)
		{
			Add(s);
		}
	}

	/// <summary>
	/// 시리즈 추가
	/// </summary>
	public void Add(ValueSeries series)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		if (_series.ContainsKey(series.Name))
			throw new ArgumentException($"Series with name '{series.Name}' already exists.");

		_series[series.Name] = series;
	}

	/// <summary>
	/// 이름으로 시리즈 생성 및 추가
	/// </summary>
	public ValueSeries CreateSeries(string name)
	{
		var series = new ValueSeries(name);
		Add(series);
		return series;
	}

	/// <summary>
	/// 시리즈 제거
	/// </summary>
	public bool Remove(string name)
	{
		return _series.Remove(name);
	}

	/// <summary>
	/// 이름으로 시리즈 조회
	/// </summary>
	public ValueSeries? GetSeries(string name)
	{
		return _series.TryGetValue(name, out var series) ? series : null;
	}

	/// <summary>
	/// 이름으로 시리즈 조회 (별칭)
	/// </summary>
	public ValueSeries? Get(string name) => GetSeries(name);

	/// <summary>
	/// 특정 시간의 모든 시리즈 값 조회
	/// </summary>
	public Dictionary<string, decimal?> GetAllValuesAt(DateTime time)
	{
		var result = new Dictionary<string, decimal?>();

		foreach (var kvp in _series)
		{
			var value = kvp.Value.GetValue(time);
			result[kvp.Key] = value?.Value;
		}

		return result;
	}

	/// <summary>
	/// 특정 시간 범위의 모든 시리즈 값들 조회
	/// </summary>
	public Dictionary<string, IEnumerable<IndicatorValue>> GetRangeValues(DateTime from, DateTime to)
	{
		var result = new Dictionary<string, IEnumerable<IndicatorValue>>();

		foreach (var kvp in _series)
		{
			var values = kvp.Value.GetRange(from, to);
			result[kvp.Key] = values;
		}

		return result;
	}

	/// <summary>
	/// 모든 시리즈의 가장 최근 값들 조회
	/// </summary>
	public Dictionary<string, decimal?> GetLastValues()
	{
		var result = new Dictionary<string, decimal?>();

		foreach (var kvp in _series)
		{
			result[kvp.Key] = kvp.Value.LastValue;
		}

		return result;
	}

	/// <summary>
	/// 모든 시리즈 데이터 정리
	/// </summary>
	public void Clear()
	{
		foreach (var series in _series.Values)
		{
			series.Clear();
		}
	}

	/// <summary>
	/// 모든 시리즈 제거
	/// </summary>
	public void ClearSeries()
	{
		_series.Clear();
	}

	/// <summary>
	/// 시리즈 이름이 존재하는지 확인
	/// </summary>
	public bool Contains(string name) => _series.ContainsKey(name);

	/// <summary>
	/// 시리즈가 존재하는지 확인
	/// </summary>
	public bool Contains(ValueSeries series) => series != null && _series.ContainsKey(series.Name);
}

