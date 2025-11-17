namespace Vectoris.Charts.Series;

/// <summary>
/// 여러 지표(LineSeries) 집합
/// </summary>
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
