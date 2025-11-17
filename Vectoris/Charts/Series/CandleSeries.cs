using Vectoris.Charts.Core;

namespace Vectoris.Charts.Series;

/// <summary>
/// 캔들 데이터 시리즈 (시계열)
/// </summary>
public class CandleSeries
{
	private readonly List<Quote> _quotes = [];

	public IReadOnlyList<Quote> Quotes => 
		_quotes;

	public void AddQuote(Quote quote)
	{
		if (_quotes.Count > 0 && quote.Time <= _quotes.Last().Time)
			throw new ArgumentException("New quote time must be greater than the last quote.");

		_quotes.Add(quote);
	}

	public Quote? GetQuote(DateTime time) => 
		_quotes.FirstOrDefault(q => q.Time == time);

	public IEnumerable<Quote> GetRange(DateTime from, DateTime to) =>
		_quotes.Where(q => q.Time >= from && q.Time <= to);
}
