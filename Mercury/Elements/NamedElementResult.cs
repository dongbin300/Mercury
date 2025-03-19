namespace Mercury.Elements
{
	public class NamedElementResult
	{
		public string Name { get; set; }
		public decimal? Value { get; set; }

		public NamedElementResult(string name, decimal? value)
		{
			Name = name;
			Value = value;
		}

		public override string ToString()
		{
			return $"{Name}, {Value}";
		}
	}
}
