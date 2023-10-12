using Newtonsoft.Json;

using System.Collections.Generic;

namespace Albedo.Models
{
    public class MaModel
    {
        public bool Enable { get; set; }
        public int Period { get; set; }
        public MaTypeModel Type { get; set; }
        public LineColorModel LineColor { get; set; } = default!;
        public LineWeightModel LineWeight { get; set; } = default!;
        [JsonIgnore]
        public List<IndicatorData> Data { get; set; } = new();

        public MaModel(bool enable, int period, MaTypeModel type, LineColorModel lineColor, LineWeightModel lineWeight)
        {
            Enable = enable;
            Period = period;
            Type = type;
            LineColor = lineColor;
            LineWeight = lineWeight;
        }
    }
}
