namespace Mercury.Indicators
{
    public class BbResult(DateTime date, double? sma, double? upper, double? lower)
	{
		public DateTime Date { get; set; } = date;
		public double? Sma { get; set; } = sma;
		public double? Upper { get; set; } = upper;
		public double? Lower { get; set; } = lower;
	}
}
