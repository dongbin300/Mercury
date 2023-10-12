using Albedo.Enums;

using System.Windows.Media;

namespace Albedo.Models
{
    public class LineColorModel
    {
        public LineColor LineColor { get; set; }
        public SolidColorBrush Color { get; set; }

        public LineColorModel(LineColor lineColor, SolidColorBrush color)
        {
            LineColor = lineColor;
            Color = color;
        }
    }
}
