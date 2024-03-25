namespace Mercury.Charts
{
	public class Price
	{
		public DateTime Date { get; set; }
		public decimal Value { get; set; }

		public Price()
		{

		}

		public Price(DateTime date, decimal value)
		{
			Date = date;
			Value = value;
		}
	}
}
