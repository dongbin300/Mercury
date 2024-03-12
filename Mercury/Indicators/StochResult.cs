namespace Mercury.Indicators
{
    public class StochResult(DateTime date, double stoch)
	{
		public DateTime Date { get; set; } = date;
		public double Stoch { get; set; } = stoch;
	}
}
