namespace Mercury.Indicators
{
    public class StdevResult(DateTime date, double? stdev)
	{
		public DateTime Date { get; set; } = date;
		public double? Stdev { get; set; } = stdev;
	}
}
