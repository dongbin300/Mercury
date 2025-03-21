namespace Mercury.Indicators
{
    /// <summary>
    /// Supertrend Result
    /// </summary>
    public class SupertrendResult(DateTime date, double? supertrend)
	{
		public DateTime Date { get; set; } = date;
		public double? Supertrend { get; set; } = supertrend;
	}
}
