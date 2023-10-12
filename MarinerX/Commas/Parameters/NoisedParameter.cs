using MarinerX.Commas.Noises;
using MarinerX.Utils;

using System;

namespace MarinerX.Commas.Parameters
{
    public class NoisedParameter
    {
        public Noise Noise { get; set; }
        public decimal Value { get; set; }

        public NoisedParameter(Noise noise, decimal value)
        {
            Noise = noise;
            Value = value;
        }

        public void Adjust(decimal noise)
        {
            var random = new SmartRandom();
            var _noise = Math.Clamp(noise * 0.9m + random.Next(10000) * noise * 0.00002m, 0, 1);
            var gap = (Noise.EvaluationMax - Noise.EvaluationMin) * _noise * 0.5m;
            Value = Math.Clamp(Math.Clamp(Value - gap, Noise.EvaluationMin, Noise.EvaluationMax) + random.Next(10000) * 0.00002m, Noise.EvaluationMin, Noise.EvaluationMax);
        }
    }
}
