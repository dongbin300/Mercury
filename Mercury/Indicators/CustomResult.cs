namespace Mercury.Indicators
{
    /// <summary>
    /// Custom Result
    /// </summary>
    public class CustomResult
    {
        public DateTime Date { get; set; }
        public double Custom { get; set; }

        public CustomResult(DateTime date, double custom)
        {
            Date = date;
			Custom = custom;
        }
    }
}
