using GeneticSharp;

using Mercury.Charts;

namespace GeneticLab.Fitnesses
{
	public class RsiTradeFitness_Account
	{
		public decimal Balance = 1_000_000;
		public decimal Btc = 0;
	}

	public class RsiTradeFitness : IFitness
	{
		public static ChartPack? Charts = null;

		public double Evaluate(IChromosome chromosome)
		{
			var values = ((FloatingPointChromosome)chromosome).ToFloatingPoints();
			var buySignalRsi = values[0];
			var sellSignalRsi = values[1];

			var account = new RsiTradeFitness_Account();

			if (Charts == null)
			{
				Charts = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.FiveMinutes);
				Charts.UseRsi();
			}

			var startTime = Charts.StartTime.AddMinutes(5 * RandomizationProvider.Current.GetInt(0, 100_000));
			var c1 = Charts.Select(startTime);
			var c0 = Charts.Next();

			for (int i = 0; i < 72; i++)
			{
				if (account.Btc <= 0 &&
					c1.Rsi1 < buySignalRsi &&
					c0.Rsi1 > buySignalRsi)
				{
					account.Balance -= c0.Quote.Close;
					account.Btc++;
				}
				else if (account.Btc > 0 &&
					c1.Rsi1 > sellSignalRsi &&
					c0.Rsi1 < sellSignalRsi)
				{
					account.Balance += c0.Quote.Close;
					account.Btc--;
				}

				c1 = c0;
				c0 = Charts.Next();
			}

			if (account.Btc > 0)
			{
				account.Btc = 0;
				account.Balance += c0.Quote.Close;
			}

			return (double)account.Balance;
		}
	}
}
