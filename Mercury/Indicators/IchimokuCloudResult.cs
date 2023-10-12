namespace Mercury.Indicators
{
    public class IchimokuCloudResult
    {
        public DateTime Date { get; set; }
        public double Conversion { get; set; }
        public double Base { get; set; }
        public double TrailingSpan { get; set; }
        public double LeadingSpan1 { get; set; }
        public double LeadingSpan2 { get; set; }

        public IchimokuCloudResult(DateTime date, double conversion, double _base, double trailingSpan, double leadingSpan1, double leadingSpan2)
        {
            Date = date;
            Conversion = conversion;
            Base = _base;
            TrailingSpan = trailingSpan;
            LeadingSpan1 = leadingSpan1;
            LeadingSpan2 = leadingSpan2;
        }
    }
}
