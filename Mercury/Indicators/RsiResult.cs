namespace Mercury.Indicators
{
    /// <summary>
    /// Relative Strength Index Result
    /// </summary>
    public class RsiResult
    {
        public DateTime Date { get; set; }
        public double Rsi { get; set; }

        public RsiResult(DateTime date, double rsi)
        {
            Date = date;
            Rsi = rsi;
        }
    }
}
