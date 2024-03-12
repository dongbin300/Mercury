namespace Mercury.Indicators
{
    /// <summary>
    /// Rubber Index result by Gaten
    /// </summary>
    public class RiResult(DateTime date, double ri)
	{
		public DateTime Date { get; set; } = date;
		public double Ri { get; set; } = ri;
	}
}
