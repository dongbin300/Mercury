namespace Mercury.Indicators
{
    public class AdxResult(DateTime date, double adx)
	{
		public DateTime Date { get; set; } = date;
		public double Adx { get; set; } = adx;
	}
}
