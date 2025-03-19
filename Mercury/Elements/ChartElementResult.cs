using Mercury.Enums;

namespace Mercury.Elements
{
	public class ChartElementResult
	{
		public MtmChartElementType Type { get; set; }
		public decimal? Value { get; set; }

		public ChartElementResult(MtmChartElementType type, decimal? value)
		{
			Type = type;
			Value = value;
		}

		public override string ToString()
		{
			return $"{Type}, {Value}";
		}
	}
}
