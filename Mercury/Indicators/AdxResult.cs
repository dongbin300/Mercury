namespace Mercury.Indicators
{
    public class AdxResult
    {
        public DateTime Date { get; set; }
        public double Adx { get; set; }

        public AdxResult(DateTime date, double adx)
        {
            Date = date;
            Adx = adx;
        }
    }
}
