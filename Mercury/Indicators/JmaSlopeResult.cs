namespace Mercury.Indicators
{
    public class JmaSlopeResult
    {
        public DateTime Date { get; set; }
        public double JmaSlope { get; set; }

        public JmaSlopeResult(DateTime date, double jmaSlope)
        {
            Date = date;
            JmaSlope = jmaSlope;
        }
    }
}
