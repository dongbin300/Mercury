namespace MarinerX.Commas.Noises
{
    public class InverseLinearNoise : Noise
    {
        public InverseLinearNoise(decimal evaluationMin, decimal evaluationMax) : base(evaluationMin, evaluationMax)
        {
        }

        public new decimal GetNoiseValue(decimal value)
        {
            var _value = base.GetNoiseValue(value);
            var _evaluation = EvaluationMax - EvaluationMin;
            return 1 - _value / _evaluation;
        }
    }
}
