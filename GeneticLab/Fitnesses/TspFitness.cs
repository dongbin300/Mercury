using GeneticLab.Chromosomes;
using GeneticLab.Models;

using GeneticSharp;

namespace GeneticLab.Fitnesses
{
    public class TspFitness : IFitness
    {
        private readonly List<City> cities;

        public TspFitness(List<City> cities)
        {
            this.cities = cities;
        }

        public double Evaluate(IChromosome chromosome)
        {
            var route = (chromosome as TspChromosome).GetCities();
            double totalDistance = 0;

            for (int i = 0; i < route.Count() - 1; i++)
            {
                var currentCity = route.ElementAt(i);
                var nextCity = route.ElementAt(i + 1);
                totalDistance += CalculateDistance(currentCity, nextCity);
            }

            // Return the inverse of distance since GeneticSharp tries to minimize the fitness value
            return 1 / totalDistance;
        }

        private double CalculateDistance(City city1, City city2)
        {
            return Math.Sqrt(Math.Pow(city2.X - city1.X, 2) + Math.Pow(city2.Y - city1.Y, 2));
        }
    }
}
