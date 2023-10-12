namespace Mercury.Indicators
{
    public class SmaResult
    {
        public DateTime Date { get; set; }
        public double Sma { get; set; }

        public SmaResult(DateTime date, double sma)
        {
            Date = date;
            Sma = sma;
        }
    }
}
