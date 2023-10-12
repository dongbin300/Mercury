using System;

namespace MarinerX.Commas.Noises
{
    public abstract class Noise
    {
        public decimal EvaluationMin { get; set; }
        public decimal EvaluationMax { get; set; }

        public Noise(decimal evaluationMin, decimal evaluationMax)
        {
            EvaluationMin = evaluationMin;
            EvaluationMax = evaluationMax;
        }

        public decimal GetNoiseValue(decimal value)
        {
            return Math.Clamp(value, EvaluationMin, EvaluationMax) - EvaluationMin;
        }
    }
}
