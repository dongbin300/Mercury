namespace Mercury.Charts
{
    public class CurrentPrice(string symbol, decimal price)
	{
		public string Symbol { get; set; } = symbol;
		public decimal Price { get; set; } = price;
	}
}
