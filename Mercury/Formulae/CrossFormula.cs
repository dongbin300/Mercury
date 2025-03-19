using Mercury.Enums;
using Mercury.Interfaces;

namespace Mercury.Formulae
{
	public class CrossFormula : Formula
	{
		public IElement Element1 { get; set; } = default!;
		public MtmCross Cross { get; set; } = MtmCross.None;
		public IElement Element2 { get; set; } = default!;

		public CrossFormula()
		{

		}

		public CrossFormula(IElement element1, MtmCross cross, IElement element2)
		{
			Element1 = element1;
			Cross = cross;
			Element2 = element2;
		}

		public override string ToString()
		{
			return Element1.ToString() + FormulaUtil.CrossToString(Cross) + Element2.ToString();
		}
	}
}
