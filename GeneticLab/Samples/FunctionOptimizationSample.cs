using GeneticLab.Fitnesses;

using GeneticSharp;

using System.Windows.Controls;
using System.Windows.Threading;

namespace GeneticLab.Samples
{
    public class FunctionOptimizationSample
    {
        public void Run(Dispatcher dispatcher, ListBox listBox)
        {
            // 3차 함수라서 array length : 3
            var chromosome = new FloatingPointChromosome(
                [-5.0, -5.0, -5.0],  // 최소값
                [5.0, 5.0, 5.0],     // 최대값
                [64, 64, 64],               // 총 비트 수
                [16, 16, 16]                // 소수 자릿수
            );

            var fitness = new CubicFunctionFitness();
            var selection = new EliteSelection();

            // 교차 전략 : Uniform Crossover
            // 균등 교차는 유전자별 50% 확률로 부모1의 유전자, 50% 확률로 부모2의 유전자를 복사해서 자식을 생성
            // ex) 부모1 : 1 2 3 4 5
            // 부모2 : 6 7 8 9 10
            // 자식1 : 1 7 3 4 10
            // 자식2 : 6 2 3 9 5
            var crossover = new UniformCrossover();

            // 변이 전략 : Flip Bit Mutation
            // 임의의 하나의 유전자 값을 Bit Flip 한다. (0 -> 1, 1 -> 0)
            var mutation = new FlipBitMutation();

            var population = new Population(50, 100, chromosome);
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);

            ga.Termination = new GenerationNumberTermination(1000);

            ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = ga.BestChromosome as FloatingPointChromosome;

                var bestFitness = bestChromosome.Fitness;
                var values = bestChromosome.ToFloatingPoints();

                dispatcher.Invoke(() =>
                {
                    listBox.Items.Insert(0, $"Best Fitness: {bestFitness}\nBest Values: ({values[0]}, {values[1]}, {values[2]})");
                });
            };

            ga.Start();
        }
    }
}
