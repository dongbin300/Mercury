namespace Mercury.Indicators
{
	public class DonchianChannelResult(DateTime date, double? basis, double? upper, double? lower)
	{
		public DateTime Date { get; set; } = date;
		public double? Basis { get; set; } = basis;
		public double? Upper { get; set; } = upper;
		public double? Lower { get; set; } = lower;
	}
}
