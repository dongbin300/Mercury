namespace Mercury.Indicators
{
    public class JmaSlopeResult(DateTime date, double jmaSlope)
	{
		public DateTime Date { get; set; } = date;
		public double JmaSlope { get; set; } = jmaSlope;
	}
}
