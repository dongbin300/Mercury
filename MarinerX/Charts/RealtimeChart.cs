using Mercury;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarinerX.Charts
{
    public class RealtimeChart
    {
        public string Symbol { get; set; }
        public List<Quote> Quotes { get; set; } = new();
        public decimal CurrentPrice => Quotes.Count == 0 ? 0m : Quotes[^1].Close;
        public double CurrentRsi { get; set; }
        public double CurrentRi { get; set; }
        public string RsiColor => CurrentRsi >= 70 ? "3BCF86" : CurrentRsi <= 30 ? "ED3161" : "FFFFFF";
        public string RiColor => CurrentRi >= 6 ? "3BCF86" : CurrentRi <= -6 ? "ED3161" : "FFFFFF";

        public RealtimeChart(string symbol, List<Quote> quotes)
        {
            Symbol = symbol;
            Quotes = quotes;
        }

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
