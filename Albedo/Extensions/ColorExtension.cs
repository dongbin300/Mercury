using SkiaSharp;

using System.Windows.Media;

namespace Albedo.Extensions
{
    public static class ColorExtension
    {
        public static SKColor ToSKColor(this SolidColorBrush brush)
        {
            return new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);
        }
    }
}
