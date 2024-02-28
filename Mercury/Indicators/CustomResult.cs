namespace Mercury.Indicators
{
	/// <summary>
	/// Custom Result
	/// </summary>
	public class CustomResult
    {
        public DateTime Date { get; set; }
        public double Upper { get; set; }
        public double Lower { get; set; }
        public double Pioneer { get; set; }
        public double Player { get; set; }

        public CustomResult(DateTime date, double upper, double lower, double pioneer, double player)
        {
            Date = date;
            Upper = upper;
            Lower = lower;
            Pioneer = pioneer;
            Player = player;
        }
    }
}
