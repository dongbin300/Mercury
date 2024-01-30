using GeneticLab.Models;

using GeneticSharp;

namespace GeneticLab.Chromosomes
{
    public class TspChromosome : ChromosomeBase
    {
        private readonly int numberOfCities;

        public TspChromosome(int numberOfCities) : base(numberOfCities)
        {
            this.numberOfCities = numberOfCities;
            CreateGenes();
        }

        public override IChromosome CreateNew()
        {
            return new TspChromosome(numberOfCities);
        }

        public IEnumerable<City> GetCities()
        {
            return GetGenes().Select(x => x.Value as City);
        }

        public override Gene GenerateGene(int geneIndex)
        {
            var random = new Random();
            return new Gene(new City($"City {geneIndex}", random.Next(0, 100), random.Next(0, 100)));
        }
    }
}
