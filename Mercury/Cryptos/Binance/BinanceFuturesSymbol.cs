using Binance.Net.Enums;

namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesSymbol(string name, decimal liquidationFee, DateTime listingDate, decimal? maxPrice, decimal? minPrice, decimal? tickSize, decimal? maxQuantity, decimal? minQuantity, decimal? stepSize, int pricePrecision, int quantityPrecision, UnderlyingType underlyingType)
	{
		public string Name { get; set; } = name;
		public decimal LiquidationFee { get; set; } = liquidationFee;
		public DateTime ListingDate { get; set; } = listingDate;
		public decimal? MaxPrice { get; set; } = maxPrice;
		public decimal? MinPrice { get; set; } = minPrice;
		public decimal? TickSize { get; set; } = tickSize;
		public decimal? MaxQuantity { get; set; } = maxQuantity;
		public decimal? MinQuantity { get; set; } = minQuantity;
		public decimal? StepSize { get; set; } = stepSize;
		public int PricePrecision { get; set; } = pricePrecision;
		public int QuantityPrecision { get; set; } = quantityPrecision;
		public UnderlyingType UnderlyingType { get; set; } = underlyingType;
	}
}
