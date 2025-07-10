namespace Mercury.Charts
{
	public class PricePack(string symbol)
	{
		public string Symbol { get; set; } = symbol;
		public Dictionary<DateTime, List<decimal>> Prices { get; set; } = [];
	}
}