namespace Vectoris.Charts.Core;

public abstract class IndicatorBase : IIndicator
{
	protected readonly List<decimal?> _values = [];
	public IReadOnlyList<decimal?> Values => _values.AsReadOnly();
	public abstract string Name { get; }
	public abstract decimal? Current { get; }

	/// <summary>
	/// 캔들 하나 추가 (실시간/배치 공용)
	/// </summary>
	public abstract void AddQuote(Quote quote);

	public void AddQuotes(IEnumerable<Quote> quotes)
	{
		foreach (var q in quotes)
			AddQuote(q);
	}
}
