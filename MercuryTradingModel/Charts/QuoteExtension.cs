using Mercury;

using static System.Math;

namespace MercuryTradingModel.Charts
{
    public static class QuoteExtension
    {
        public static double CloseAverage(this List<Quote> quotes, int currentIndex, int period)
        {
            double sum = 0;
            period = Min(period, currentIndex);
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
            period = Min(period, currentIndex);
            for (int i = 0; i < period; i++)
            {
                sum += Pow(Convert.ToDouble(quotes[currentIndex - i].Close) - average, 2);
            }
            return sum / period;
        }

        public static double CloseStandardDeviation(this List<Quote> quotes, int currentIndex, int period, double average)
        {
            double sum = 0;
            period = Min(period, currentIndex);
            for (int i = 0; i < period; i++)
            {
                sum += Pow(Convert.ToDouble(quotes[currentIndex - i].Close) - average, 2);
            }
            return sum / period;
        }
    }
}
