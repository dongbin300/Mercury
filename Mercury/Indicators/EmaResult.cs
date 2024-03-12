namespace Mercury.Indicators
{
    public class EmaResult(DateTime date, double ema)
	{
		public DateTime Date { get; set; } = date;
		public double Ema { get; set; } = ema;
	}
}
