using Albedo.Models;

using SkiaSharp;

using System.Collections.Generic;

namespace Albedo.Extensions
{
    public static class SkiaSharpExtension
    {
        public static void DrawColoredText(this SKCanvas canvas, IEnumerable<SKColoredText> text, float x, float y, SKFont font, float xMargin)
        {
            float startX = x;
            float currentX = x;
            float currentY = y;
            foreach (SKColoredText textItem in text)
            {
                if (textItem == SKColoredText.NewLine)
                {
                    currentY += font.Size;
                    currentX = startX;
                    continue;
                }

                canvas.DrawText(
                    textItem.Text,
                    currentX,
                    currentY,
                    font,
                    new SKPaint() { Color = textItem.TextColor });

                currentX += textItem.Text.Length * (font.Size + (textItem.Margin == null ? xMargin : textItem.Margin.Value));
            }
        }
    }
}
