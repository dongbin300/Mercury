using Mercury.Assets;
using Mercury.Charts;
using Mercury.Formulae;
using Mercury.Interfaces;
using Mercury.Signals;

using Newtonsoft.Json;

namespace Mercury.Cues
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

		public virtual bool CheckFlare(Asset asset, ChartInfo chart, ChartInfo prevChart)
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
