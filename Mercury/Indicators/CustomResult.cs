namespace Mercury.Indicators
{
	/// <summary>
	/// Custom Result
	/// </summary>
	public class CustomResult(DateTime date, double? upper, double? lower, double? pioneer, double? player)
	{
		public DateTime Date { get; set; } = date;
		public double? Upper { get; set; } = upper;
		public double? Lower { get; set; } = lower;
		public double? Pioneer { get; set; } = pioneer;
		public double? Player { get; set; } = player;
	}
}
