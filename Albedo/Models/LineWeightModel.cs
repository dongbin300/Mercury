using Albedo.Enums;

namespace Albedo.Models
{
    public class LineWeightModel
    {
        public LineWeight LineWeight { get; set; }
        public string Text { get; set; }

        public LineWeightModel(LineWeight lineWeight, string text)
        {
            LineWeight = lineWeight;
            Text = text;
        }
    }
}
