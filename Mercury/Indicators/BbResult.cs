namespace Mercury.Indicators
{
    public class BbResult
    {
        public DateTime Date { get; set; }
        public double Sma { get; set; }
        public double Upper { get; set; }
        public double Lower { get; set; }

        public BbResult(DateTime date, double sma, double upper, double lower)
        {
            Date = date;
            Sma = sma;
            Upper = upper;
            Lower = lower;
        }
    }
}
