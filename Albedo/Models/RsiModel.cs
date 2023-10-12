using Newtonsoft.Json;

using System.Collections.Generic;

namespace Albedo.Models
{
    public class RsiModel
    {
        public bool Enable { get; set; }
        public int Period { get; set; }
        public LineColorModel LineColor { get; set; } = default!;
        public LineWeightModel LineWeight { get; set; } = default!;
        [JsonIgnore]
        public List<IndicatorData> Data { get; set; } = new();

        public RsiModel(bool enable, int period, LineColorModel lineColor, LineWeightModel lineWeight)
        {
            Enable = enable;
            Period = period;
            LineColor = lineColor;
            LineWeight = lineWeight;
        }
    }
}
