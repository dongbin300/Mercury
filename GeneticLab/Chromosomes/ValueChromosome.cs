using GeneticSharp;

namespace GeneticLab.Chromosomes
{
    public class ValueChromosome : ChromosomeBase
    {
        public ValueChromosome() : base(10)
        {
            CreateGenes();
        }

        /// <summary>
        /// 유전자 생산
        /// CreateGenes()에 의해 호출된다.
        /// </summary>
        /// <param name="geneIndex"></param>
        /// <returns></returns>
        public override Gene GenerateGene(int geneIndex)
        {
            return new Gene(new Random().NextDouble());
        }

        /// <summary>
        /// 새로운 염색체 생산
        /// 교차할 때 호출된다.
        /// </summary>
        /// <returns></returns>
        public override IChromosome CreateNew()
        {
            return new ValueChromosome();
        }
    }
}
