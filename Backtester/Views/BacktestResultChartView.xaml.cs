using Binance.Net.Enums;

using Mercury.Backtests;
using Mercury.Charts;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Backtester.Views
{
    /// <summary>
    /// BacktestResultChartView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BacktestResultChartView : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("User32.dll")]
        public static extern bool GetCursorPos(ref POINT lpPoint);

        public static Point GetCursorPosition()
        {
            var lpPoint = new POINT();
            GetCursorPos(ref lpPoint);
            return new Point(lpPoint.X, lpPoint.Y);
        }

        private readonly SKFont GridTextFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 9);
        private readonly SKFont CandleInfoFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 11);
        private readonly SKFont CurrentTickerFont = new(SKTypeface.FromFamilyName("Meiryo UI", SKFontStyle.Bold), 11);
        private readonly SKPaint GridPaint = new() { Color = new SKColor(45, 45, 45) };
        private readonly SKPaint GridTextPaint = new() { Color = new SKColor(65, 65, 65) };
        private readonly SKPaint CandleInfoPaint = new() { Color = SKColors.White };
        private readonly SKPaint LongPaint = new() { Color = new(59, 207, 134) };
        private readonly SKPaint ShortPaint = new() { Color = new(237, 49, 97) };
        private readonly SKPaint CandlePointerPaint = new() { Color = new SKColor(255, 255, 255, 32) };
        private readonly SKPaint CandleBuyPointerPaint = new() { Color = new SKColor(59, 207, 134, 64) };
        private readonly SKPaint CandleSellPointerPaint = new() { Color = new SKColor(237, 49, 97, 64) };
        private readonly int CandleTopBottomMargin = 30;

        public List<PositionHistory> PositionHistories { get; set; } = new();
        public int CurrentHistoryIndex = 0;
        public PositionHistory CurrentHistory => PositionHistories[CurrentHistoryIndex];
        public int ChartCount { get; set; }
        public int HistoryCount => PositionHistories.Count;
        public KlineInterval MainInterval { get; set; }
        public int EntryIndex { get; set; }
        public int ExitIndex { get; set; }

        public int ViewCountMin = 30;
        public int ViewCountMax = 2000;

        public float CurrentMouseX;

        public BacktestResultChartView()
        {
            InitializeComponent();
        }

        public void Init(List<PositionHistory> histories, KlineInterval interval)
        {
            PositionHistories = histories;
            MainInterval = interval;
            CurrentHistoryIndex = 0;
            Title = $"{CurrentHistoryIndex + 1}/{HistoryCount}, {CurrentHistory.Symbol}, {CurrentHistory.Side}, {CurrentHistory.Result}, {Math.Round(CurrentHistory.Income, 4)}";
            Render();
        }

        #region Quote
        private void IndexButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentHistoryIndex = int.Parse(IndexTextBox.Text) - 1;
            Title = $"{CurrentHistoryIndex + 1}/{HistoryCount}, {CurrentHistory.Symbol}, {CurrentHistory.Side}, {CurrentHistory.Result}, {Math.Round(CurrentHistory.Income, 4)}";
            Render();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentHistoryIndex == 0)
            {
                return;
            }

            PrevChart();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentHistoryIndex >= HistoryCount - 1)
            {
                return;
            }

            NextChart();
        }

        public void PrevChart()
        {
            CurrentHistoryIndex--;
            Title = $"{CurrentHistoryIndex + 1}/{HistoryCount}, {CurrentHistory.Symbol}, {CurrentHistory.Side}, {CurrentHistory.Result}, {Math.Round(CurrentHistory.Income, 4)}";
            Render();
        }

        public void NextChart()
        {
            CurrentHistoryIndex++;
            Title = $"{CurrentHistoryIndex + 1}/{HistoryCount}, {CurrentHistory.Symbol}, {CurrentHistory.Side}, {CurrentHistory.Result}, {Math.Round(CurrentHistory.Income, 4)}";
            Render();
        }
        #endregion

        #region View Info
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var cursorPosition = GetCursorPosition();
                var x = (float)cursorPosition.X - (float)CandleChart.PointToScreen(new Point(0, 0)).X;

                if (x < 0 || x >= CandleChart.ActualWidth - CandleChart.ActualWidth / ChartCount)
                {
                    if (CurrentMouseX != -1358)
                    {
                        CurrentMouseX = -1358;
                        Render();
                    }
                    return;
                }

                CurrentMouseX = x;
                Render();
            }
            catch
            {
            }
        }
        #endregion

        #region Main Render
        public void Render()
        {
            CandleChart.InvalidateVisual();
            IndicatorChart.InvalidateVisual();
        }
        #endregion

        #region Chart Content Render
        private void CandleChart_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (HistoryCount <= 0)
            {
                return;
            }

            var start = CurrentHistory.EntryTime.AddHours(-3);
            var end = CurrentHistory.Time.AddHours(3);
            var charts = ChartLoader.GetChartPack(CurrentHistory.Symbol, MainInterval).GetCharts(start, end);
            EntryIndex = charts.IndexOf(charts.Find(x => x.DateTime.Equals(CurrentHistory.EntryTime)) ?? default!);
            ExitIndex = charts.IndexOf(charts.Find(x => x.DateTime.Equals(CurrentHistory.Time)) ?? default!);
            ChartCount = charts.Count;

            var actualWidth = (float)CandleChart.ActualWidth;
            var actualHeight = (float)CandleChart.ActualHeight - CandleTopBottomMargin * 2;
            var actualItemFullWidth = actualWidth / ChartCount;
            var actualItemMargin = actualItemFullWidth * 0.2f;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var yMax = Math.Max(charts.Max(x => Math.Abs(x.Ema1 ?? 0)), (double)charts.Max(x => x.Quote.High));
            var yMin = Math.Min(charts.Min(x => Math.Abs(x.Ema1 ?? 0)), (double)charts.Min(x => x.Quote.Low));

            //var yMax = Math.Max(charts.Max(x => Math.Abs(x.Supertrend3)), Math.Max(charts.Max(x => Math.Abs(x.Supertrend2)), Math.Max(charts.Max(x => Math.Abs(x.Supertrend1)), (double)charts.Max(x => x.Quote.High))));
            //var yMax = Math.Max(charts.Max(x => Math.Abs(x.Supertrend3)), Math.Max(charts.Max(x => Math.Abs(x.Supertrend2)), Math.Max(charts.Max(x => Math.Abs(x.Supertrend1)), Math.Max(charts.Max(x => x.Ema1), (double)charts.Max(x => x.Quote.High)))));
            //var yMin = Math.Min(charts.Min(x => Math.Abs(x.Supertrend3)), Math.Min(charts.Min(x => Math.Abs(x.Supertrend2)), Math.Min(charts.Min(x => Math.Abs(x.Supertrend1)), (double)charts.Min(x => x.Quote.Low))));
            //var yMin = Math.Min(charts.Min(x => Math.Abs(x.Supertrend3)), Math.Min(charts.Min(x => Math.Abs(x.Supertrend2)), Math.Min(charts.Min(x => Math.Abs(x.Supertrend1)), Math.Min(charts.Min(x => x.Ema1), (double)charts.Min(x => x.Quote.Low)))));
            //var digit = 4;

            // Draw Grid
            //var gridLevel = 4; // 4등분
            //for (int i = 0; i <= gridLevel; i++)
            //{
            //    canvas.DrawLine(
            //        new SKPoint(0, actualHeight * ((float)i / gridLevel) + CandleTopBottomMargin),
            //        new SKPoint(actualWidth, actualHeight * ((float)i / gridLevel) + CandleTopBottomMargin),
            //        GridPaint
            //        );
            //}

            // Draw Quote and Indicator
            for (int i = 0; i < charts.Count; i++)
            {
                var quote = charts[i].Quote;
                var ema = charts[i].Ema1;
                //var st1 = charts[i].Supertrend1;
                //var st2 = charts[i].Supertrend2;
                //var st3 = charts[i].Supertrend3;
                var preEma = i == 0 ? charts[i].Ema1 : charts[i - 1].Ema1;
                //var preSt1 = i == 0 ? charts[i].Supertrend1 : charts[i - 1].Supertrend1;
                //var preSt2 = i == 0 ? charts[i].Supertrend2 : charts[i - 1].Supertrend2;
                //var preSt3 = i == 0 ? charts[i].Supertrend3 : charts[i - 1].Supertrend3;
                var viewIndex = i;

                // EMA
                //canvas.DrawLine(
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex - 0.5f),
                //        actualHeight * (float)(1.0 - (preEma - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex + 0.5f),
                //        actualHeight * (float)(1.0 - (ema - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPaint() { Color = SKColors.White }
                //    );

                // Supertrend
                //canvas.DrawLine(
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex - 0.5f),
                //        actualHeight * (float)(1.0 - (Math.Abs(preEma) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex + 0.5f),
                //        actualHeight * (float)(1.0 - (Math.Abs(ema) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPaint() { Color = ema > 0 ? SKColors.Green : SKColors.Red }
                //    );
                //canvas.DrawLine(
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex - 0.5f),
                //        actualHeight * (float)(1.0 - (Math.Abs(preSt2) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex + 0.5f),
                //        actualHeight * (float)(1.0 - (Math.Abs(st2) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPaint() { Color = st2 > 0 ? SKColors.Green : SKColors.Red }
                //    );
                //canvas.DrawLine(
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex - 0.5f),
                //        actualHeight * (float)(1.0 - (Math.Abs(preSt3) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPoint(
                //        actualItemFullWidth * (viewIndex + 0.5f),
                //        actualHeight * (float)(1.0 - (Math.Abs(st3) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                //    new SKPaint() { Color = st3 > 0 ? SKColors.Green : SKColors.Red }
                //    );

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - ((double)quote.High - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - ((double)quote.Low - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    quote.Open < quote.Close ? LongPaint : ShortPaint);
                canvas.DrawRect(
                    new SKRect(
                        actualItemFullWidth * viewIndex + actualItemMargin / 2,
                        actualHeight * (float)(1.0 - ((double)quote.Open - yMin) / (yMax - yMin)) + CandleTopBottomMargin,
                        actualItemFullWidth * (viewIndex + 1) - actualItemMargin / 2,
                        actualHeight * (float)(1.0 - ((double)quote.Close - yMin) / (yMax - yMin)) + CandleTopBottomMargin
                        ),
                    quote.Open < quote.Close ? LongPaint : ShortPaint
                    );
            }

            // Draw Trade Pointer
            canvas.DrawRect(
                EntryIndex * actualItemFullWidth,
                0,
                actualItemFullWidth,
                (float)CandleChart.ActualHeight,
                CurrentHistory.Side == PositionSide.Long ? CandleBuyPointerPaint : CandleSellPointerPaint
                );
            canvas.DrawRect(
                ExitIndex * actualItemFullWidth,
                0,
                actualItemFullWidth,
                (float)CandleChart.ActualHeight,
                CurrentHistory.Side == PositionSide.Long ? CandleSellPointerPaint : CandleBuyPointerPaint
                );

            // Draw Pointer
            canvas.DrawRect(
                (int)(CurrentMouseX / actualItemFullWidth) * actualItemFullWidth,
                0,
                actualItemFullWidth,
                (float)CandleChart.ActualHeight,
                CandlePointerPaint
                );

            // Draw Info Text
            try
            {
                var pointingChart = CurrentMouseX == -1358 ? charts[ChartCount - 1] : charts[(int)(CurrentMouseX / actualItemFullWidth)];
                var changeText = pointingChart.Quote.Close >= pointingChart.Quote.Open ? $"+{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}" : $"{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}";
                canvas.DrawText($"{pointingChart.DateTime:yyyy-MM-dd HH:mm:ss}, O {pointingChart.Quote.Open} H {pointingChart.Quote.High} L {pointingChart.Quote.Low} C {pointingChart.Quote.Close}", 3, 10, CandleInfoFont, CandleInfoPaint);



                //canvas.DrawText($"ST1 {Math.Round(pointingChart.Supertrend1, digit)} ST2 {Math.Round(pointingChart.Supertrend2, digit)} ST3 {Math.Round(pointingChart.Supertrend3, digit)}", 3, 23, CandleInfoFont, CandleInfoPaint);

                //canvas.DrawText($"EMA200 {Math.Round(pointingChart.Ema1, digit)} ST1 {Math.Round(pointingChart.Supertrend1, digit)} ST2 {Math.Round(pointingChart.Supertrend2, digit)} ST3 {Math.Round(pointingChart.Supertrend3, digit)} K {Math.Round(pointingChart.K, digit)} D {Math.Round(pointingChart.D, digit)}", 3, 23, CandleInfoFont, CandleInfoPaint);
            }
            catch
            {
            }
        }

        private void VolumeChart_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            //if (HistoryCount <= 0)
            //{
            //    return;
            //}

            //var start = CurrentHistory.EntryTime.AddHours(-12);
            //var end = CurrentHistory.Time.AddHours(12);
            //var charts = ChartLoader.GetChartPack(CurrentHistory.Symbol, MainInterval).GetCharts(start, end);

            //var actualWidth = (float)IndicatorChart.ActualWidth;
            //var actualHeight = (float)IndicatorChart.ActualHeight - VolumeTopBottomMargin * 2;
            //var actualItemFullWidth = actualWidth / ViewItemCount;

            //var canvas = e.Surface.Canvas;
            //canvas.Clear(SKColors.Transparent);

            //var volumeMax = Quotes.Skip(StartItemIndex).Take(ViewItemCount).Max(x => x.Volume);

            //// Draw Grid
            //var gridLevel = 2; // 2등분
            //for (int i = 0; i <= gridLevel; i++)
            //{
            //    canvas.DrawLine(
            //        new SKPoint(0, actualHeight * ((float)i / gridLevel) + VolumeTopBottomMargin),
            //        new SKPoint(actualWidth, actualHeight * ((float)i / gridLevel) + VolumeTopBottomMargin),
            //        GridPaint
            //        );
            //}

            //// Draw Candle Pointer
            //canvas.DrawRect(
            //    (int)(CurrentMouseX / actualItemFullWidth) * actualItemFullWidth,
            //    0,
            //    actualItemFullWidth,
            //    (float)IndicatorChart.ActualHeight,
            //    CandlePointerPaint
            //    );

            //for (int i = StartItemIndex; i < EndItemIndex; i++)
            //{
            //    var quote = Quotes[i];
            //    var viewIndex = i - StartItemIndex;

            //    // Draw Volume Histogram
            //    canvas.DrawRect(
            //        new SKRect(
            //            actualItemFullWidth * viewIndex + ActualItemMargin / 2,
            //            actualHeight * (float)(1.0m - quote.Volume / volumeMax) + VolumeTopBottomMargin,
            //            actualItemFullWidth * (viewIndex + 1) - ActualItemMargin / 2,
            //            actualHeight + VolumeTopBottomMargin
            //            ),
            //        quote.Open < quote.Close ? LongPaint : ShortPaint
            //        );
            //}
        }
        #endregion
    }
}
