namespace Mercury.Indicators
{
    public class IchimokuCloudResult(DateTime date, double conversion, double _base, double trailingSpan, double leadingSpan1, double leadingSpan2)
	{
		public DateTime Date { get; set; } = date;
		public double Conversion { get; set; } = conversion;
		public double Base { get; set; } = _base;
		public double TrailingSpan { get; set; } = trailingSpan;
		public double LeadingSpan1 { get; set; } = leadingSpan1;
		public double LeadingSpan2 { get; set; } = leadingSpan2;
	}
}
