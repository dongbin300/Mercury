namespace Mercury.Indicators
{
    /// <summary>
    /// Time Segmented Volume Result
    /// </summary>
    public class TsvResult(DateTime date, double tsv)
	{
		public DateTime Date { get; set; } = date;
		public double Tsv { get; set; } = tsv;
	}
}
