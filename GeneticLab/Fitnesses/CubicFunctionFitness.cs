using GeneticSharp;

namespace GeneticLab.Fitnesses
{
    public class CubicFunctionFitness : IFitness
    {
        public double Evaluate(IChromosome chromosome)
        {
            var values = ((FloatingPointChromosome)chromosome).ToFloatingPoints();
            var x = values[0];
            var y = values[1];
            var z = values[2];

            // x^3z - 2x^2y^2 + 3xyz 최적화
            return (x * x * x * z) - (2 * x * x * y * y) + (3 * x * y * z);
        }
    }
}
