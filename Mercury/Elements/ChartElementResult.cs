using Mercury.Enums;

namespace Mercury.Elements
{
	public class ChartElementResult(MtmChartElementType type, decimal? value)
	{
		public MtmChartElementType Type { get; set; } = type;
		public decimal? Value { get; set; } = value;

		public override string ToString()
		{
			return $"{Type}, {Value}";
		}
	}
}
