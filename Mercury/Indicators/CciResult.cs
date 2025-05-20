namespace Mercury.Indicators
{
    public class CciResult(DateTime date, double? cci)
	{
		public DateTime Date { get; set; } = date;
		public double? Cci { get; set; } = cci;
	}
}
