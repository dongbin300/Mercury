namespace Mercury.Elements
{
	public class NamedElementResult(string name, decimal? value)
	{
		public string Name { get; set; } = name;
		public decimal? Value { get; set; } = value;

		public override string ToString()
		{
			return $"{Name}, {Value}";
		}
	}
}
