namespace Mercury.Indicators
{
    /// <summary>
    /// LSMA(Least Square Moving Average) Result
    /// </summary>
    public class LsmaResult
    {
        public DateTime Date { get; set; }
        public double Lsma { get; set; }

        public LsmaResult(DateTime date, double lsma)
        {
            Date = date;
            Lsma = lsma;
        }
    }
}
