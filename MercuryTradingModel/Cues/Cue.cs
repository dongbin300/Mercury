using MercuryTradingModel.Assets;
using MercuryTradingModel.Charts;
using MercuryTradingModel.Formulae;
using MercuryTradingModel.Interfaces;
using MercuryTradingModel.Signals;

using Newtonsoft.Json;

namespace MercuryTradingModel.Cues
{
    public class Cue : ICue
    {
        public IFormula Formula { get; set; } = new Formula();
        public int Life { get; set; }
        [JsonIgnore]
        public int CurrentLife { get; set; }

        public Cue()
        {

        }

        public Cue(IFormula formula, int life)
        {
            Formula = formula;
            Life = life;
        }

        public virtual bool CheckFlare(Asset asset, MercuryChartInfo chart, MercuryChartInfo prevChart)
        {
            var formula = new Signal(Formula);
            if (formula.IsFlare(asset, chart, prevChart))
            {
                CurrentLife = Life;
                return true;
            }

            return --CurrentLife > 0;
        }

        public virtual void Expire()
        {
            CurrentLife = 0;
        }

        public override string ToString()
        {
            return Formula.ToString() + " " + Life.ToString();
        }
    }
}
