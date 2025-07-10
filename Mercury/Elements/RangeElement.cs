using Mercury.Interfaces;

namespace Mercury.Elements
{
	public class RangeElement(decimal startValue, decimal endValue) : IElement
	{
		public decimal StartValue { get; set; } = startValue;
		public decimal EndValue { get; set; } = endValue;

		public bool IsValid(decimal num)
		{
			return num >= StartValue && num <= EndValue;
		}

		public override string ToString()
		{
			return StartValue.ToString() + "~" + EndValue.ToString();
		}
	}
}
