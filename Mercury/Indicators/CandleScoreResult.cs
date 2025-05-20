namespace Mercury.Indicators
{
    public class CandleScoreResult(DateTime date, double? candleScore)
	{
		public DateTime Date { get; set; } = date;
		public double? CandleScore { get; set; } = candleScore;
	}
}
