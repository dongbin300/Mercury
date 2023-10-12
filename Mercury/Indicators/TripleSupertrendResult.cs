namespace Mercury.Indicators
{
    /// <summary>
    /// Triple Supertrend Result
    /// </summary>
    public class TripleSupertrendResult
    {
        public DateTime Date { get; set; }
        public double Supertrend1 { get; set; }
        public double Supertrend2 { get; set; }
        public double Supertrend3 { get; set; }

        public TripleSupertrendResult(DateTime date, double supertrend1, double supertrend2, double supertrend3)
        {
            Date = date;
            Supertrend1 = supertrend1;
            Supertrend2 = supertrend2;
            Supertrend3 = supertrend3;
        }
    }
}
