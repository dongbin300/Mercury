namespace Vectoris.Charts.Series;

/// <summary>
/// 하위 호환성을 위한 IndicatorSeries (향후 제거 권장)
/// </summary>
[Obsolete("Use SeriesCollection instead. This class will be removed in future versions.")]
public class IndicatorSeries
{
	private readonly List<LineSeries> _lines = [];

	public IReadOnlyList<LineSeries> Lines =>
		_lines;

	public void AddLine(LineSeries line)
	{
		if (_lines.Any(l => l.Name == line.Name))
			throw new ArgumentException($"Line with name '{line.Name}' already exists.");

		_lines.Add(line);
	}

	public LineSeries? GetLine(string name) =>
		_lines.FirstOrDefault(l => l.Name == name);
}
