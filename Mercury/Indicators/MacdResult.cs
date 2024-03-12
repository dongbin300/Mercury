namespace Mercury.Indicators
{
    public class MacdResult(DateTime date, double macd, double signal, double hist)
	{
		public DateTime Date { get; set; } = date;
		public double Macd { get; set; } = macd;
		public double Signal { get; set; } = signal;
		public double Hist { get; set; } = hist;
	}
}
