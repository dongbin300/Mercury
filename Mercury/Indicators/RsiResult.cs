namespace Mercury.Indicators
{
    /// <summary>
    /// Relative Strength Index Result
    /// </summary>
    public class RsiResult(DateTime date, double? rsi)
	{
		public DateTime Date { get; set; } = date;
		public double? Rsi { get; set; } = rsi;
	}
}
