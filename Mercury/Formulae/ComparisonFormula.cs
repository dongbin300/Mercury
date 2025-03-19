using Mercury.Enums;
using Mercury.Interfaces;

namespace Mercury.Formulae
{
	public class ComparisonFormula : Formula
	{
		public IElement Element1 { get; set; } = default!;
		public MtmComparison Comparison { get; set; } = MtmComparison.None;
		public IElement Element2 { get; set; } = default!;

		public ComparisonFormula()
		{

		}

		public ComparisonFormula(IElement element1, MtmComparison comparison, IElement element2)
		{
			Element1 = element1;
			Comparison = comparison;
			Element2 = element2;
		}

		public override string ToString()
		{
			return Element1.ToString() + FormulaUtil.ComparisonToString(Comparison) + Element2.ToString();
		}
	}
}
