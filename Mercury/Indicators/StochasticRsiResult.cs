namespace Mercury.Indicators
{
    /// <summary>
    /// Stochastic RSI Result
    /// </summary>
    public class StochasticRsiResult(DateTime date, double? k, double? d)
	{
		public DateTime Date { get; set; } = date;
		public double? K { get; set; } = k;
		public double? D { get; set; } = d;
	}
}
