using SkiaSharp;

namespace Albedo.Models
{
    public class SKColoredText
    {
        public static SKColoredText NewLine { get; set; } = default!;

        public string Text { get; set; }
        public SKColor TextColor { get; set; }
        public float? Margin { get; set; }

        public SKColoredText(string text, SKColor fontColor, float? margin = null)
        {
            Text = text;
            TextColor = fontColor;
            Margin = margin;
        }
    }
}
