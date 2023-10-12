using Newtonsoft.Json;

using System.Collections.Generic;

namespace Albedo.Models
{
    public class IcModel
    {
        public bool Enable { get; set; }
        public int ShortPeriod { get; set; }
        public int MidPeriod { get; set; }
        public int LongPeriod { get; set; }
        public bool CloudEnable { get; set; }
        public LineColorModel TenkanLineColor { get; set; } = default!;
        public LineColorModel KijunLineColor { get; set; } = default!;
        public LineColorModel ChikouLineColor { get; set; } = default!;
        public LineColorModel Senkou1LineColor { get; set; } = default!;
        public LineColorModel Senkou2LineColor { get; set; } = default!;
        public LineWeightModel TenkanLineWeight { get; set; } = default!;
        public LineWeightModel KijunLineWeight { get; set; } = default!;
        public LineWeightModel ChikouLineWeight { get; set; } = default!;
        public LineWeightModel Senkou1LineWeight { get; set; } = default!;
        public LineWeightModel Senkou2LineWeight { get; set; } = default!;
        [JsonIgnore]
        public List<IndicatorData> TenkanData { get; set; } = new();
        [JsonIgnore]
        public List<IndicatorData> KijunData { get; set; } = new();
        [JsonIgnore]
        public List<IndicatorData> ChikouData { get; set; } = new();
        [JsonIgnore]
        public List<IndicatorData> Senkou1Data { get; set; } = new();
        [JsonIgnore]
        public List<IndicatorData> Senkou2Data { get; set; } = new();

        public IcModel(bool enable, int shortPeriod, int midPeriod, int longPeriod, bool cloudEnable, LineColorModel tenkanLineColor, LineColorModel kijunLineColor, LineColorModel chikouLineColor, LineColorModel senkou1LineColor, LineColorModel senkou2LineColor, LineWeightModel tenkanLineWeight, LineWeightModel kijunLineWeight, LineWeightModel chikouLineWeight, LineWeightModel senkou1LineWeight, LineWeightModel senkou2LineWeight)
        {
            Enable = enable;
            ShortPeriod = shortPeriod;
            MidPeriod = midPeriod;
            LongPeriod = longPeriod;
            CloudEnable = cloudEnable;
            TenkanLineColor = tenkanLineColor;
            KijunLineColor = kijunLineColor;
            ChikouLineColor = chikouLineColor;
            Senkou1LineColor = senkou1LineColor;
            Senkou2LineColor = senkou2LineColor;
            TenkanLineWeight = tenkanLineWeight;
            KijunLineWeight = kijunLineWeight;
            ChikouLineWeight = chikouLineWeight;
            Senkou1LineWeight = senkou1LineWeight;
            Senkou2LineWeight = senkou2LineWeight;
        }
    }
}
