using Mercury.Extensions;

namespace Mercury.Charts
{
    public class RealtimeChart(string symbol, List<Quote> quotes)
	{
		public string Symbol { get; set; } = symbol;
		public List<Quote> Quotes { get; set; } = quotes;
		public decimal CurrentPrice => Quotes.Count == 0 ? 0m : Quotes[^1].Close;
		public double CurrentRsi { get; set; }
		public double CurrentRi { get; set; }
		public string RsiColor => CurrentRsi >= 70 ? "3BCF86" : CurrentRsi <= 30 ? "ED3161" : "FFFFFF";
		public string RiColor => CurrentRi >= 6 ? "3BCF86" : CurrentRi <= -6 ? "ED3161" : "FFFFFF";

		public void UpdateQuote(Quote quote)
		{
			var _quote = Quotes.Find(q => q.Date.Equals(quote.Date));
			if (_quote == null)
			{
				Quotes.Add(quote);
			}
			else
			{
				_quote.Open = quote.Open;
				_quote.High = quote.High;
				_quote.Low = quote.Low;
				_quote.Close = quote.Close;
				_quote.Volume = quote.Volume;
			}
		}

		public void CalculateIndicators()
		{
			CurrentRsi = Math.Round(Quotes.TakeLast(15).GetRsi().Last().Rsi, 2);
			CurrentRi = Math.Round(Quotes.TakeLast(15).GetRi(14).Last().Ri, 2);
		}
	}
}
