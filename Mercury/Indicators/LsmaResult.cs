namespace Mercury.Indicators
{
    /// <summary>
    /// LSMA(Least Square Moving Average) Result
    /// </summary>
    public class LsmaResult(DateTime date, double? lsma)
	{
		public DateTime Date { get; set; } = date;
		public double? Lsma { get; set; } = lsma;
	}
}
