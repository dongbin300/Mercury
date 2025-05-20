namespace Mercury.Indicators
{
    public class VolatilityRatioResult(DateTime date, double? volatilityRatio)
	{
		public DateTime Date { get; set; } = date;
		public double? VolatilityRatio { get; set; } = volatilityRatio;
	}
}
