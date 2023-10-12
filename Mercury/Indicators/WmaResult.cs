namespace Mercury.Indicators
{
    public class WmaResult
    {
        public DateTime Date { get; set; }
        public double Wma { get; set; }

        public WmaResult(DateTime date, double wma)
        {
            Date = date;
            Wma = wma;
        }
    }
}
