using GeneticLab.Chromosomes;
using GeneticLab.Fitnesses;
using GeneticLab.Models;

using GeneticSharp;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GeneticLab.Samples
{
    public class TspSample
    {
        private const int CityRadius = 5;

        public void Run(Dispatcher dispatcher, Canvas canvas)
        {
            var cities = new List<City>
            {
                new City("City A", 50, 50),
                new City("City B", 100, 200),
                new City("City C", 200, 100),
                new City("City D", 350, 200),
                new City("City E", 400, 50)
            };

            dispatcher.Invoke(() =>
            {
                foreach (var city in cities)
                {
                    var ellipse = new Ellipse
                    {
                        Width = CityRadius * 2,
                        Height = CityRadius * 2,
                        Fill = Brushes.Blue
                    };

                    Canvas.SetLeft(ellipse, city.X - CityRadius);
                    Canvas.SetTop(ellipse, city.Y - CityRadius);

                    canvas.Children.Add(ellipse);
                }

                var chromosome = new TspChromosome(cities.Count);
                var fitness = new TspFitness(cities);
                var selection = new EliteSelection();

                // 교차 전략 : Ordered Crossover
                // 순서 교차는 유전자의 순서가 중요한 염색체에 적합함
                var crossover = new OrderedCrossover();

                // 변이 전략 : Reverse Sequence Mutation
                // RSM 변이는 유전자 순서를 반대로 뒤집는다.
                // ex) 1 2 3 4 5
                // => 5 4 3 2 1
                var mutation = new ReverseSequenceMutation();

                var population = new Population(50, 100, chromosome);
                var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);

                ga.Termination = new GenerationNumberTermination(100);

                ga.GenerationRan += (sender, e) =>
                {
                    var bestChromosome = ga.BestChromosome as TspChromosome;

                    var routeLine = new Polyline
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2
                    };

                    foreach (var city in bestChromosome.GetCities())
                    {
                        routeLine.Points.Add(new Point(city.X, city.Y));
                    }

                    canvas.Children.Add(routeLine);
                };

                ga.Start();
            });
           
        }
    }
}
