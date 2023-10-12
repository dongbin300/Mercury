namespace Mercury.Indicators
{
    public class EmaResult
    {
        public DateTime Date { get; set; }
        public double Ema { get; set; }

        public EmaResult(DateTime date, double ema)
        {
            Date = date;
            Ema = ema;
        }
    }
}
