namespace Mercury.Indicators
{
    public class StochResult
    {
        public DateTime Date { get; set; }
        public double Stoch { get; set; }

        public StochResult(DateTime date, double stoch)
        {
            Date = date;
            Stoch = stoch;
        }
    }
}
