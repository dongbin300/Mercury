using GeneticLab.Chromosomes;
using GeneticLab.Fitnesses;

using GeneticSharp;

using System.Windows.Controls;
using System.Windows.Threading;

namespace GeneticLab.Samples
{
    public class SumSample
    {
        public void Run(Dispatcher dispatcher, ListBox listBox)
        {
            // 염색체 : 여러 개의 유전자(Gene)을 가지고 있음
            var chromosome = new ValueChromosome();

            // 적합도 : 염색체의 점수를 매기는 기준
            var fitness = new ValueFitness();

            // 선택 전략 : Elite Selection
            // 점수가 가장 높은 n개를 선택
            var selection = new EliteSelection();

            // 교차 전략 : One Point Crossover
            // 특정 지점에서 교차해서 자식을 생성
            // ex)
            // 부모1 : 1000101101011110
            // 부모2 : 0010101110100010
            // 인덱스 10에서 교차
            // => 1000101101 + 100010
            // => 1000101101100010
            var crossover = new OnePointCrossover();

            // 변이 전략 : Twors Mutation
            // 염색체의 임의의 두 유전자를 교체
            // ex)
            // 염색체(5) : 17 4 9 81 34
            // 변이(1, 4)
            // => 17 34 9 81 4
            var mutation = new TworsMutation();

            // 인구 : 한 세대에서의 염색체 개수
            // 최소 50, 최대 100, 기본 염색체는 정의해서 넣어줘야 함
            var population = new Population(50, 100, chromosome);

            // 유전 알고리즘
            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);

            // 종료 전략 : Generation Number Termination
            // n세대가 되면 종료한다
            ga.Termination = new GenerationNumberTermination(10000);

            // 한 세대가 끝날 때 마다 해줄 작업 등록
            ga.GenerationRan += (sender, e) =>
            {
                var bestChromosome = ga.BestChromosome as ValueChromosome;
                dispatcher.Invoke(() =>
                {
                    listBox.Items.Insert(0, $"Generation {ga.GenerationsNumber} - Best Fitness: {bestChromosome.Fitness}\n");
                });
            };

            // 유전 알고리즘 시작
            ga.Start();

            // 유전 알고리즘 최종 결과
            var bestResult = ga.BestChromosome as ValueChromosome;
            dispatcher.Invoke(() =>
            {
                listBox.Items.Insert(0, $"Best Result - Fitness: {bestResult.Fitness}\n");
            });
        }
    }
}
