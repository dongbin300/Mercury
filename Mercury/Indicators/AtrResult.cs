namespace Mercury.Indicators
{
	public class AtrResult(DateTime date, double? atr)
	{
		public DateTime Date { get; set; } = date;
		public double? Atr { get; set; } = atr;
	}
}
