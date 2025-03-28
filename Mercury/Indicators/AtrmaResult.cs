namespace Mercury.Indicators
{
	public class AtrmaResult(DateTime date, double? atrma)
	{
		public DateTime Date { get; set; } = date;
		public double? Atrma { get; set; } = atrma;
	}
}
