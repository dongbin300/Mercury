namespace Mercury.Indicators
{
	/// <summary>
	/// TrendRider Result
	/// </summary>
	public class TrendRiderResult(DateTime date, double? trend, double? supertrend)
	{
		public DateTime Date { get; set; } = date;
		public double? Trend { get; set; } = trend;
		public double? Supertrend { get; set; } = supertrend;
	}
}
