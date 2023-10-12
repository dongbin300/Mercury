namespace Mercury.Indicators
{
    /// <summary>
    /// Time Segmented Volume Result
    /// </summary>
    public class TsvResult
    {
        public DateTime Date { get; set; }
        public double Tsv { get; set; }

        public TsvResult(DateTime date, double tsv)
        {
            Date = date;
            Tsv = tsv;
        }
    }
}
