using Mercury;

namespace TradeBot.Models
{
    public class ChartInfo
    {
        public Quote Quote { get; set; }
        public double Macd { get; set; }
        public double Signal { get; set; }
        public double Supertrend { get; set; }
        public double Adx { get; set; }
        public double Stoch { get; set; }

        public ChartInfo(Quote quote)
        {
            Quote = quote;
        }
    }
}
