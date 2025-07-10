using Mercury.Interfaces;

namespace Mercury.Elements
{
	public class ValueElement(decimal value) : IElement
	{
		public decimal Value { get; set; } = value;

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}
