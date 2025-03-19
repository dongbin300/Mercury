using System;

namespace MarinerX.Markets
{
	public class SymbolBenchmark2(string symbol, DateTime listingDate, decimal priceTickSize, decimal currentPrice, decimal priceTickPer, int elapsedDays, int maxLeverage, string pass)
	{
		public string Symbol { get; set; } = symbol;
		public DateTime ListingDate { get; set; } = listingDate;
		public decimal PriceTickSize { get; set; } = priceTickSize;
		public decimal CurrentPrice { get; set; } = currentPrice;
		public decimal PriceTickPer { get; set; } = priceTickPer;
		public int ElapsedDays { get; set; } = elapsedDays;
		public int MaxLeverage { get; set; } = maxLeverage;
		public string MaxLeverageString => "X" + MaxLeverage;
		public string Pass { get; set; } = pass;

		public string ToCopyString()
		{
			return $"{Symbol},{ListingDate},{PriceTickSize},{CurrentPrice},{PriceTickPer},{ElapsedDays},{MaxLeverageString},{Pass}";
		}
	}
}