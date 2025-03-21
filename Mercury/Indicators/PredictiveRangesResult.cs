namespace Mercury.Indicators
{
	/// <summary>
	/// Predictive Ranges Result
	/// </summary>
	public class PredictiveRangesResult(DateTime date, double? u2, double? u, double? avg, double? l, double? l2)
	{
		public DateTime Date { get; set; } = date;
		public double? Upper2 { get; set; } = u2;
		public double? Upper { get; set; } = u;
		public double? Average { get; set; } = avg;
		public double? Lower { get; set; } = l;
		public double? Lower2 { get; set; } = l2;
	}
}
