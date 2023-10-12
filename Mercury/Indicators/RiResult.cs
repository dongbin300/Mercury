namespace Mercury.Indicators
{
    /// <summary>
    /// Rubber Index result by Gaten
    /// </summary>
    public class RiResult
    {
        public DateTime Date { get; set; }
        public double Ri { get; set; }

        public RiResult(DateTime date, double ri)
        {
            Date = date;
            Ri = ri;
        }
    }
}
