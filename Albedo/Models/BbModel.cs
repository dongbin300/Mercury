using Newtonsoft.Json;

using System.Collections.Generic;

namespace Albedo.Models
{
    public class BbModel
    {
        public bool Enable { get; set; }
        public int Period { get; set; }
        public float Deviation { get; set; }
        public LineColorModel SmaLineColor { get; set; } = default!;
        public LineColorModel UpperLineColor { get; set; } = default!;
        public LineColorModel LowerLineColor { get; set; } = default!;
        public LineWeightModel SmaLineWeight { get; set; } = default!;
        public LineWeightModel UpperLineWeight { get; set; } = default!;
        public LineWeightModel LowerLineWeight { get; set; } = default!;
        [JsonIgnore]
        public List<IndicatorData> SmaData { get; set; } = new();
        [JsonIgnore]
        public List<IndicatorData> UpperData { get; set; } = new();
        [JsonIgnore]
        public List<IndicatorData> LowerData { get; set; } = new();

        public BbModel(bool enable, int period, float deviation, LineColorModel smaLineColor, LineWeightModel smaLineWeight, LineColorModel upperLineColor, LineWeightModel upperLineWeight, LineColorModel lowerLineColor, LineWeightModel lowerLineWeight)
        {
            Enable = enable;
            Period = period;
            Deviation = deviation;
            SmaLineColor = smaLineColor;
            SmaLineWeight = smaLineWeight;
            UpperLineColor = upperLineColor;
            UpperLineWeight = upperLineWeight;
            LowerLineColor = lowerLineColor;
            LowerLineWeight = lowerLineWeight;
        }
    }
}
