namespace Mercury.Indicators
{
    public class MacdResult
    {
        public DateTime Date { get; set; }
        public double Macd { get; set; }
        public double Signal { get; set; }
        public double Hist { get; set; }

        public MacdResult(DateTime date, double macd, double signal, double hist)
        {
            Date = date;
            Macd = macd;
            Signal = signal;
            Hist = hist;
        }
    }
}
