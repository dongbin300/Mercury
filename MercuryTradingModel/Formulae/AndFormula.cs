using MercuryTradingModel.Interfaces;

namespace MercuryTradingModel.Formulae
{
    public class AndFormula : Formula
    {
        public IFormula? Formula1 { get; set; }
        public IFormula? Formula2 { get; set; }

        public AndFormula(IFormula? formula1, IFormula? formula2)
        {
            Formula1 = formula1;
            Formula2 = formula2;
        }

        public override string ToString()
        {
            return Formula1?.ToString() + " and " + Formula2?.ToString();
        }
    }
}
