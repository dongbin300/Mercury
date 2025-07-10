using Mercury.Interfaces;

namespace Mercury.Formulae
{
	public class OrFormula(IFormula? formula1, IFormula? formula2) : Formula
	{
		public IFormula? Formula1 { get; set; } = formula1;
		public IFormula? Formula2 { get; set; } = formula2;

		public override string ToString()
		{
			return Formula1?.ToString() + " or " + Formula2?.ToString();
		}
	}
}
