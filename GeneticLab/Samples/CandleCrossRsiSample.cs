using GeneticLab.Fitnesses;

using GeneticSharp;

using Mercury.Charts;

using System.Windows.Controls;
using System.Windows.Threading;

namespace GeneticLab.Samples
{
	public class CandleCrossRsiSample
	{
		public void Run(Dispatcher dispatcher, ListBox listBox)
		{
			// BTCUSDT 2023-01-01 ~ 2023-12-31 5분봉 차트 초기화
			ChartLoader.InitChartsMByDate("BTCUSDT", Binance.Net.Enums.KlineInterval.FiveMinutes,
				new DateTime(2023, 1, 1),
				new DateTime(2023, 12, 31));

			// RSI Goldencross value, RSI Deadcross value
			var chromosome = new FloatingPointChromosome(
				[0, 0], // 최소값
				[100, 100], // 최대값
				[64, 64], // 총 비트 수
				[16, 16] // 소수 자릿수
			);

			var fitness = new RsiTradeFitness();
			var selection = new EliteSelection();
			var crossover = new OnePointCrossover();
			var mutation = new FlipBitMutation();
			var population = new Population(50, 100, chromosome);
			var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
			{
				Termination = new GenerationNumberTermination(1000)
			};

			ga.GenerationRan += (sender, e) =>
			{
				var bestChromosome = ga.BestChromosome as FloatingPointChromosome;

				var bestFitness = bestChromosome.Fitness;
				var values = bestChromosome.ToFloatingPoints();

				dispatcher.Invoke(() =>
				{
					listBox.Items.Insert(0, $"[{ga.GenerationsNumber}] Best: {bestFitness}\n(Buy Signal: {values[0]}, Sell Signal: {values[1]})");
				});
			};

			ga.Start();
		}
	}
}
