namespace Mercury.Indicators
{
    /// <summary>
    /// Triple Supertrend Result
    /// </summary>
    public class TripleSupertrendResult(DateTime date, double supertrend1, double supertrend2, double supertrend3)
	{
		public DateTime Date { get; set; } = date;
		public double Supertrend1 { get; set; } = supertrend1;
		public double Supertrend2 { get; set; } = supertrend2;
		public double Supertrend3 { get; set; } = supertrend3;
	}
}
