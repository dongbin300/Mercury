namespace Mercury.Extensions
{
	public static class QuoteExtension
	{
		public static double CloseAverage(this List<Quote> quotes, int currentIndex, int period)
		{
			double sum = 0;
			period = Math.Min(period, currentIndex);
			for (int i = 0; i < period; i++)
			{
				sum += Convert.ToDouble(quotes[currentIndex - i].Close);
			}
			return sum / period;
		}

		public static double CloseStandardDeviation(this List<Quote> quotes, int currentIndex, int period)
		{
			double average = CloseAverage(quotes, currentIndex, period);

			double sum = 0;
			period = Math.Min(period, currentIndex);
			for (int i = 0; i < period; i++)
			{
				sum += Math.Pow(Convert.ToDouble(quotes[currentIndex - i].Close) - average, 2);
			}
			return sum / period;
		}

		public static double CloseStandardDeviation(this List<Quote> quotes, int currentIndex, int period, double average)
		{
			double sum = 0;
			period = Math.Min(period, currentIndex);
			for (int i = 0; i < period; i++)
			{
				sum += Math.Pow(Convert.ToDouble(quotes[currentIndex - i].Close) - average, 2);
			}
			return sum / period;
		}
	}
}
