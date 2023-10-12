namespace Mercury.Indicators
{
    /// <summary>
    /// Supertrend Result
    /// </summary>
    public class SupertrendResult
    {
        public DateTime Date { get; set; }
        public double Supertrend { get; set; }

        public SupertrendResult(DateTime date, double supertrend)
        {
            Date = date;
            Supertrend = supertrend;
        }
    }
}
