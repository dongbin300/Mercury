namespace Mercury.Indicators
{
    /// <summary>
    /// Stochastic RSI Result
    /// </summary>
    public class StochasticRsiResult
    {
        public DateTime Date { get; set; }
        public double K { get; set; }
        public double D { get; set; }

        public StochasticRsiResult(DateTime date, double k, double d)
        {
            Date = date;
            K = k;
            D = d;
        }
    }
}
