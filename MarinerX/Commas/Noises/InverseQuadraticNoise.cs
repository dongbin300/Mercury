namespace MarinerX.Commas.Noises
{
    public class InverseQuadraticNoise : Noise
    {
        public InverseQuadraticNoise(decimal evaluationMin, decimal evaluationMax) : base(evaluationMin, evaluationMax)
        {
        }

        public new decimal GetNoiseValue(decimal value)
        {
            var _value = base.GetNoiseValue(value) * base.GetNoiseValue(value);
            var _evaluation = EvaluationMax - EvaluationMin;
            return 1 - _value / (_evaluation * _evaluation);
        }
    }
}
