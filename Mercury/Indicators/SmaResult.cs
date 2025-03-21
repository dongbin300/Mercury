namespace Mercury.Indicators
{
    public class SmaResult(DateTime date, double? sma)
	{
		public DateTime Date { get; set; } = date;
		public double? Sma { get; set; } = sma;
	}
}
