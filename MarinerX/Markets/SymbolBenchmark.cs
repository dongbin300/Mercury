using System;

namespace MarinerX.Markets
{
    public class SymbolBenchmark
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Volatility { get; set; } = decimal.Zero;
        public decimal MarketCapWon { get; set; } = decimal.Zero;
        public int MaxLeverage { get; set; } = 0;
        public string ListingDate { get; set; } = string.Empty;
        public string MaxLeverageString => "X" + MaxLeverage;
        public double BenchmarkScore => CalculateBenchmarkScore();
        public double ForceBenchmarkScore => CalculateForceBenchmarkScore();

        public SymbolBenchmark(string symbol, decimal volatility, decimal marketCapWon, int maxLeverage, string listingDate)
        {
            Symbol = symbol;
            Volatility = volatility;
            MarketCapWon = marketCapWon;
            MaxLeverage = maxLeverage;
            ListingDate = listingDate;
        }

        public double CalculateBenchmarkScore()
        {
            return Math.Round(Math.Pow((double)MarketCapWon, 0.25) * (1 / (double)Volatility) * 100);
        }

        public double CalculateForceBenchmarkScore()
        {
            return Math.Round((1_000_000 / (Math.Pow((double)MarketCapWon, 0.25) * (double)Volatility)));
        }

        public string ToCopyString()
        {
            return $"{Symbol},{Volatility},{MarketCapWon},{MaxLeverageString},{ListingDate},{BenchmarkScore}";
        }
    }
}
