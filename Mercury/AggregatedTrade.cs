namespace Mercury
{
	public class AggregatedTrade(DateTime date, decimal price, decimal quantity)
	{
		public DateTime Date { get; set; } = date;
		public decimal Price { get; set; } = price;
		public decimal Quantity { get; set; } = quantity;
	}
}
