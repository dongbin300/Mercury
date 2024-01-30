using GeneticSharp;

namespace GeneticLab.Fitnesses
{
    public class ValueFitness : IFitness
    {
        /// <summary>
        /// 염색체의 적합도 평가
        /// </summary>
        /// <param name="chromosome"></param>
        /// <returns></returns>
        public double Evaluate(IChromosome chromosome)
        {
            var genes = chromosome.GetGenes();
            double sum = 0;

            // 점수 = 모든 유전자의 합
            foreach (var gene in genes)
            {
                sum += (double)gene.Value;
            }

            return sum;
        }
    }
}
