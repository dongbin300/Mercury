using SkiaSharp;

namespace Albedo.Utils
{
    public class DrawingTools
    {
        public static readonly SKColor BaseColor = SKColors.White;
        public static readonly SKColor LongColor = new(59, 207, 134);
        public static readonly SKColor ShortColor = new(237, 49, 97);

        public static readonly SKFont GridTextFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 9);
        public static readonly SKFont CandleInfoFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 11);
        public static readonly SKFont CurrentTickerFont = new(SKTypeface.FromFamilyName("Meiryo UI", SKFontStyle.Bold), 11);

        public static readonly SKPaint GridPaint = new() { Color = new SKColor(45, 45, 45) };
        public static readonly SKPaint GridTextPaint = new() { Color = new SKColor(65, 65, 65) };
        public static readonly SKPaint CandleInfoPaint = new() { Color = BaseColor };
        public static readonly SKPaint LongPaint = new() { Color = LongColor };
        public static readonly SKPaint ShortPaint = new() { Color = ShortColor };
        public static readonly SKPaint CandlePointerPaint = new() { Color = new SKColor(255, 255, 255, 32) };
    }
}
