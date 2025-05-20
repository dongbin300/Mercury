namespace Mercury.Indicators
{
    public class EwmacResult(DateTime date, double? ewmac)
	{
		public DateTime Date { get; set; } = date;
		public double? Ewmac { get; set; } = ewmac;
	}
}
