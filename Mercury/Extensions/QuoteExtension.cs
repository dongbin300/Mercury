using Mercury.Charts;

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

		public static ChartInfo GetLatestChartBefore(this IList<ChartInfo> charts, DateTime dateTime) => charts.Last(x => x.DateTime < dateTime);
		public static List<ChartInfo> GetChartsBefore(this IList<ChartInfo> charts, DateTime dateTime, int count)
		{
			if (charts.Count == 0)
				return [];

			int left = 0;
			int right = charts.Count - 1;
			int lastIndex = -1;

			while (left <= right)
			{
				int mid = (left + right) / 2;
				if (charts[mid].DateTime <= dateTime)
				{
					lastIndex = mid;
					left = mid + 1;
				}
				else
				{
					right = mid - 1;
				}
			}

			if (lastIndex == -1)
				return [];

			int start = Math.Max(0, lastIndex - count + 1);
			int length = lastIndex - start + 1;

			var result = new List<ChartInfo>(length);
			for (int i = lastIndex; i >= start; i--)
			{
				result.Add(charts[i]);
			}

			return result;
		}

	}
}
