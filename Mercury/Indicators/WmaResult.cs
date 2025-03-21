namespace Mercury.Indicators
{
    public class WmaResult(DateTime date, double? wma)
	{
		public DateTime Date { get; set; } = date;
		public double? Wma { get; set; } = wma;
	}
}
