using Albedo.Enums;
using Albedo.Extensions;
using Albedo.Managers;
using Albedo.Models;
using Albedo.Utils;

using Mercury;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using static Albedo.Apis.WinApi;

namespace Albedo.Views
{
    /// <summary>
    /// ChartControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChartControl : UserControl
    {
        private DispatcherTimer chartControlTimer = new();
        private GlobalMouseEvents mouseEvents = new();

        public List<Quote> Quotes = new();
        public int TotalCount => Quotes.Count;

        public float ChartWidth => Quotes.Count * ItemFullWidth;
        public float ViewStartPosition { get; set; } = 0;
        public float ViewEndPosition { get; set; } = 0;
        public float ViewWidth => ViewEndPosition - ViewStartPosition;

        public int ItemFullWidth => Common.ChartItemFullWidth;
        public float ItemMarginPercent => Common.ChartItemMarginPercent;
        public float ItemWidth => ItemFullWidth * (1 - ItemMarginPercent);
        public float ItemMargin => ItemFullWidth * ItemMarginPercent;

        public int StartItemIndex => (int)(Quotes.Count * (ViewStartPosition / ChartWidth));
        public int EndItemIndex => (int)(Quotes.Count * (ViewEndPosition / ChartWidth));
        public int ViewItemCount => EndItemIndex - StartItemIndex + 1;

        public float ActualItemFullWidth => (float)ActualWidth / ViewItemCount;
        public float ActualItemWidth => ActualItemFullWidth * (1 - ItemMarginPercent);
        public float ActualItemMargin => ActualItemFullWidth * ItemMarginPercent;

        public int ViewCountMin = 10;
        public int ViewCountMax = 2000;

        public Point StartMousePosition;
        public float CurrentMouseX;

        public ChartControl()
        {
            InitializeComponent();
            chartControlTimer.Interval = TimeSpan.FromMilliseconds(5);
            chartControlTimer.Tick += ChartControlTimer_Tick;
            mouseEvents.MouseLeftButtonDown += MouseEvents_MouseLeftButtonDown;
            mouseEvents.MouseLeftButtonUp += MouseEvents_MouseLeftButtonUp;
            Common.CalculateIndicators = CalculateIndicators;
        }

        public void Init(List<Quote> quotes)
        {
            Quotes = quotes;
            ViewStartPosition = ChartWidth - ItemFullWidth * 60;
            ViewEndPosition = ChartWidth;

            CalculateIndicators();
            Render();
        }

        #region Quote
        public void UpdateQuote(Quote quote)
        {
            var lastQuote = Quotes[^1];
            if (lastQuote.Date.Equals(quote.Date))
            {
                lastQuote.High = quote.High;
                lastQuote.Low = quote.Low;
                lastQuote.Close = quote.Close;
                lastQuote.Volume = quote.Volume;
            }
            else
            {
                Quotes.Add(quote);
                ViewStartPosition += ItemFullWidth;
                ViewEndPosition += ItemFullWidth;
            }

            CalculateIndicators();
            Render();
        }

        /// <summary>
        /// Merge quote in real time
        /// </summary>
        /// <param name="quote"></param>
        /// <param name="fromInterval"></param>
        /// <param name="toInterval"></param>
        public void UpdateQuote(Quote quote, CandleInterval toInterval)
        {
            var backtrackCount = 0;
            backtrackCount = toInterval switch
            {
                CandleInterval.OneWeek => (int)quote.Date.DayOfWeek,
                CandleInterval.OneMonth => quote.Date.Day - 1,
                _ => toInterval switch
                {
                    CandleInterval.ThreeMinutes => quote.Date.Minute % 3, // 1m * 3
                    CandleInterval.FiveMinutes => quote.Date.Minute % 5, // 1m * 5
                    CandleInterval.TenMinutes => quote.Date.Minute % 2, // 5m * 2
                    CandleInterval.FifteenMinutes => quote.Date.Minute % 3, // 5m * 3
                    CandleInterval.ThirtyMinutes => quote.Date.Minute % 6, // 5m * 6 | 10m * 3 | 15m * 2
                    _ => 0
                },
            };
            var lastQuote = Quotes[^1];
            if (backtrackCount == 0)
            {
                if (lastQuote.Date.Equals(quote.Date)) // Update quote
                {
                    lastQuote.High = quote.High;
                    lastQuote.Low = quote.Low;
                    lastQuote.Close = quote.Close;
                    lastQuote.Volume = quote.Volume;
                }
                else // New quote
                {
                    Quotes.Add(quote);
                    ViewStartPosition += ItemFullWidth;
                    ViewEndPosition += ItemFullWidth;
                }
            }
            else // Merge with past quotes
            {
                var backtrackQuotes = Quotes.TakeLast(backtrackCount);
                lastQuote.High = Math.Max(backtrackQuotes.Max(x => x.High), quote.High);
                lastQuote.Low = Math.Min(backtrackQuotes.Min(x => x.Low), quote.Low);
                lastQuote.Close = quote.Close;
                lastQuote.Volume = backtrackQuotes.Sum(x => x.Volume) + quote.Volume;
            }

            CalculateIndicators();
            Render();
        }

        /// <summary>
        /// Update quote whenever an order is placed (for Bithumb)
        /// </summary>
        /// <param name="price"></param>
        public void UpdateQuote(CandleInterval interval, decimal price, decimal volume)
        {
            var now = DateTime.Now;
            var lastQuote = Quotes[^1];
            var intervalSeconds = interval switch
            {
                CandleInterval.OneMinute => 60,
                CandleInterval.ThreeMinutes => 180,
                CandleInterval.FiveMinutes => 300,
                CandleInterval.TenMinutes => 600,
                CandleInterval.FifteenMinutes => 900,
                CandleInterval.ThirtyMinutes => 1800,
                CandleInterval.OneHour => 3600,
                CandleInterval.OneDay => 86400,
                CandleInterval.OneWeek => 604800,
                CandleInterval.OneMonth => 2592000,
                _ => 60
            };

            if ((now - lastQuote.Date).TotalSeconds >= intervalSeconds) // New quote
            {
                var quote = new Quote
                {
                    Date = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
                    Open = price,
                    High = price,
                    Low = price,
                    Close = price,
                    Volume = volume
                };
                Quotes.Add(quote);
                ViewStartPosition += ItemFullWidth;
                ViewEndPosition += ItemFullWidth;
            }
            else // Accumulate in last quote
            {
                lastQuote.High = Math.Max(lastQuote.High, price);
                lastQuote.Low = Math.Min(lastQuote.Low, price);
                lastQuote.Close = price;
                lastQuote.Volume += volume;
            }

            CalculateIndicators();
            Render();
        }

        public void ConcatenateQuotes(List<Quote> quotes)
        {
            var preQuoteCount = Quotes.Count;
            for (int i = quotes.Count - 1; i >= 0; i--)
            {
                var quote = quotes[i];
                var _quote = Quotes.Find(q => q.Date.Equals(quote.Date));
                if (_quote == null)
                {
                    Quotes.Insert(0, quote);
                }
            }
            var additionalQuoteCount = Quotes.Count - preQuoteCount;
            ViewStartPosition += additionalQuoteCount * ItemFullWidth;
            ViewEndPosition += additionalQuoteCount * ItemFullWidth;

            CalculateIndicators();
            Render();
        }
        #endregion

        #region Indicator
        public void CalculateIndicators()
        {
            foreach (var ma in SettingsMan.Indicators.Mas)
            {
                if (!ma.Enable)
                {
                    continue;
                }

                var period = ma.Period;
                switch (ma.Type.Type)
                {
                    case MaType.Sma:
                        ma.Data = Quotes.GetSma(period).Select(r => new IndicatorData(r.Date, r.Sma)).ToList();
                        break;

                    case MaType.Wma:
                        // TODO
                        //ma.Data = Quotes.GetWma(period)
                        //   .Select(r => r.Wma == null ?
                        //   new IndicatorData(r.Date, Common.NullValue) :
                        //   new IndicatorData(r.Date, (decimal)r.Wma.Value))
                        //   .ToList();
                        break;

                    case MaType.Ema:
                        ma.Data = Quotes.GetEma(period).Select(r => new IndicatorData(r.Date, r.Ema)).ToList();
                        break;
                }
            }
            foreach (var bb in SettingsMan.Indicators.Bbs)
            {
                if (!bb.Enable)
                {
                    continue;
                }

                var bbResult = Quotes.GetBollingerBands(bb.Period, bb.Deviation);
                bb.SmaData = bbResult.Select(r => new IndicatorData(r.Date, r.Sma)).ToList();
                bb.UpperData = bbResult.Select(r => new IndicatorData(r.Date, r.Upper)).ToList();
                bb.LowerData = bbResult.Select(r => new IndicatorData(r.Date, r.Lower)).ToList();
            }

            var ic = SettingsMan.Indicators.Ic;
            if (ic.Enable)
            {
                var icResult = Quotes.GetIchimokuCloud(ic.ShortPeriod, ic.MidPeriod, ic.LongPeriod);
                ic.TenkanData = icResult.Select(r => new IndicatorData(r.Date, r.Conversion)).ToList();
                ic.KijunData = icResult.Select(r => new IndicatorData(r.Date, r.Base)).ToList();
                ic.ChikouData = icResult.Select(r => new IndicatorData(r.Date, r.TrailingSpan)).ToList();
                ic.Senkou1Data = icResult.Select(r => new IndicatorData(r.Date, r.LeadingSpan1)).ToList();
                ic.Senkou2Data = icResult.Select(r => new IndicatorData(r.Date, r.LeadingSpan2)).ToList();
            }

            var rsi = SettingsMan.Indicators.Rsi;
            if (rsi.Enable)
            {
                var rsiResult = Quotes.GetRsi(rsi.Period);
                rsi.Data = rsiResult.Select(r => new IndicatorData(r.Date, r.Rsi)).ToList();
            }
        }
        #endregion

        #region User Event

        #region Zoom
        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var unit = (ViewEndPosition - ViewStartPosition) * 0.15f;
            if (e.Delta > 0) // Zoom-in
            {
                if (ViewItemCount <= ViewCountMin)
                {
                    return;
                }

                ViewStartPosition += unit;
            }
            else // Zoom-out
            {
                if (ViewItemCount >= ViewCountMax)
                {
                    return;
                }

                ViewStartPosition = Math.Max(0, ViewStartPosition - unit);
            }

            Render();
        }
        #endregion

        #region Scroll
        private void MouseEvents_MouseLeftButtonDown(object? sender, EventArgs e)
        {
            StartMousePosition = GetCursorPosition();

            var topLeftPoint = new Point(0, 0);
            var bottomRightPoint = new Point(ActualWidth, ActualHeight);

            var transformMatrix = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            topLeftPoint = transformMatrix.Transform(PointToScreen(topLeftPoint));
            bottomRightPoint = transformMatrix.Transform(PointToScreen(bottomRightPoint));

            // 차트 화면을 누른 경우
            if (StartMousePosition.X > topLeftPoint.X && StartMousePosition.X < bottomRightPoint.X && StartMousePosition.Y > topLeftPoint.Y && StartMousePosition.Y < bottomRightPoint.Y)
            {
                chartControlTimer.Start();
            }
        }

        private void MouseEvents_MouseLeftButtonUp(object? sender, EventArgs e)
        {
            chartControlTimer.Stop();
        }

        private void ChartControlTimer_Tick(object? sender, EventArgs e)
        {
            var currentMousePosition = GetCursorPosition();
            Vector diff = currentMousePosition - StartMousePosition;

            if (diff.X == 0)
            {
                return;
            }

            StartMousePosition = currentMousePosition;
            var movePosition = (float)diff.X / ActualItemFullWidth * ItemFullWidth * 1.3f;

            if (diff.X > 0) // Graph Move Left
            {
                if (ViewStartPosition - movePosition < 0) // Reach left-end
                {
                    movePosition = ViewStartPosition;
                }
                ViewStartPosition -= movePosition;
                ViewEndPosition -= movePosition;
                Render();
            }
            else if (diff.X < 0) // Graph Move Right
            {
                if (ViewEndPosition - movePosition > ChartWidth) // Reach right-end
                {
                    movePosition = ViewEndPosition - ChartWidth;
                }
                ViewStartPosition -= movePosition;
                ViewEndPosition -= movePosition;
                Render();
            }
        }
        #endregion

        #region View Info
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var cursorPosition = GetCursorPosition();
                var x = (float)cursorPosition.X - (float)CandleChart.PointToScreen(new Point(0, 0)).X;

                if (x < 0 || x >= CandleChart.ActualWidth - CandleChart.ActualWidth / ViewItemCount)
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

        #endregion

        #region Main Render
        public void Render()
        {
            foreach (var radioButton in IntervalGrid.Children.OfType<RadioButton>().Where(radioButton => radioButton.CommandParameter.Equals(Albedo.Settings.Default.Interval)))
            {
                radioButton.IsChecked = true;
                break;
            }

            if (TotalCount > 0 && ViewStartPosition <= ItemFullWidth)
            {
                Common.ChartAdditionalLoad.Invoke();
            }

            CandleChart.InvalidateVisual();
            CandleChartAxis.InvalidateVisual();
            VolumeChart.InvalidateVisual();
            VolumeChartAxis.InvalidateVisual();
        }
        #endregion

        #region Chart Content Render
        private double GetIndicatorMax(List<IndicatorData> data)
        {
            var values = data.Skip(StartItemIndex).Take(ViewItemCount).Where(x => x.Value != 0);
            if (values == null || !values.Any())
            {
                return 0;
            }
            return values.Max(x => x.Value);
        }

        private double GetIndicatorMin(List<IndicatorData> data)
        {
            var values = data.Skip(StartItemIndex).Take(ViewItemCount).Where(x => x.Value != 0);
            if (values == null || !values.Any())
            {
                return 0;
            }
            return values.Min(x => x.Value);
        }

        private (double, double) GetYMaxMin()
        {
            var priceMax = (double)Quotes.Skip(StartItemIndex).Take(ViewItemCount).Max(x => x.High);
            var priceMin = (double)Quotes.Skip(StartItemIndex).Take(ViewItemCount).Min(x => x.Low);
            double indicatorMax = 0;
            double indicatorMin = 99999999;
            foreach (var ma in SettingsMan.Indicators.Mas)
            {
                var max = GetIndicatorMax(ma.Data);
                var min = GetIndicatorMin(ma.Data);
                min = min == 0 ? 99999999 : min;
                indicatorMax = Math.Max(indicatorMax, max);
                indicatorMin = Math.Min(indicatorMin, min);
            }
            foreach (var bb in SettingsMan.Indicators.Bbs)
            {
                var max = GetIndicatorMax(bb.UpperData);
                var min = GetIndicatorMin(bb.LowerData);
                min = min == 0 ? 99999999 : min;
                indicatorMax = Math.Max(indicatorMax, max);
                indicatorMin = Math.Min(indicatorMin, min);
            }
            {
                var ic = SettingsMan.Indicators.Ic;
                var max1 = GetIndicatorMax(ic.Senkou1Data);
                var max2 = GetIndicatorMax(ic.Senkou2Data);
                var min1 = GetIndicatorMin(ic.Senkou1Data);
                var min2 = GetIndicatorMin(ic.Senkou2Data);
                min1 = min1 == 0 ? 99999999 : min1;
                min2 = min2 == 0 ? 99999999 : min2;
                indicatorMax = NumberUtil.Max(indicatorMax, max1, max2);
                indicatorMin = NumberUtil.Min(indicatorMin, min1, min2);
            }

            var yMax = Math.Max(priceMax, indicatorMax);
            var yMin = Math.Min(priceMin, indicatorMin);

            return (yMax, yMin);
        }

        private void DrawIndicatorLine(SKCanvas canvas, IndicatorData preIndicator, IndicatorData indicator, int viewIndex, float actualItemFullWidth, float actualHeight, double yMax, double yMin, SKPaint paint)
        {
            if (preIndicator != null && indicator != null && preIndicator.Value != 0 && indicator.Value != 0)
            {
                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - (preIndicator.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - (indicator.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin),
                    paint
                    );
            }
        }

        private void CandleChart_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (ViewItemCount <= 1)
            {
                return;
            }

            var actualWidth = (float)CandleChart.ActualWidth;
            var actualHeight = (float)CandleChart.ActualHeight - Common.CandleTopBottomMargin * 2;
            var actualItemFullWidth = actualWidth / ViewItemCount;
            var actualItemMargin = actualItemFullWidth * ItemMarginPercent;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            (var yMax, var yMin) = GetYMaxMin();
            var significantDigit = NumberUtil.GetSignificantDigitCount(Quotes[^1].Open);

            // Draw Grid
            var gridLevel = 4; // 4등분
            for (int i = 0; i <= gridLevel; i++)
            {
                canvas.DrawLine(
                    new SKPoint(0, actualHeight * ((float)i / gridLevel) + Common.CandleTopBottomMargin),
                    new SKPoint(actualWidth, actualHeight * ((float)i / gridLevel) + Common.CandleTopBottomMargin),
                    DrawingTools.GridPaint
                    );
            }

            // Draw Indicators
            var mas = SettingsMan.Indicators.Mas;
            var bbs = SettingsMan.Indicators.Bbs;
            var ic = SettingsMan.Indicators.Ic;
            var rsi = SettingsMan.Indicators.Rsi;

            // Draw Ichimoku Cloud
            if (ic.Enable)
            {
                if (ic.CloudEnable) // Cloud Mode
                {
                    var isFirstSenkou = true;
                    var senkouPath = new SKPath();
                    var firstSenkou = ic.Senkou1Data.ValueOf(StartItemIndex);
                    if (firstSenkou.Value != 0)
                    {
                        isFirstSenkou = false;
                        senkouPath.MoveTo(actualItemFullWidth * 0.5f, actualHeight * (float)(1.0 - (firstSenkou.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin);
                    }
                    for (int i = StartItemIndex; i < EndItemIndex; i++)
                    {
                        var viewIndex = i - StartItemIndex;
                        var senkou = ic.Senkou1Data.ValueOf(i);
                        if (senkou.Value == 0)
                        {
                            continue;
                        }
                        if (isFirstSenkou)
                        {
                            isFirstSenkou = false;
                            senkouPath.MoveTo(actualItemFullWidth * (viewIndex + 0.5f), actualHeight * (float)(1.0 - (senkou.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin);
                        }
                        else
                        {
                            senkouPath.LineTo(actualItemFullWidth * (viewIndex + 0.5f), actualHeight * (float)(1.0 - (senkou.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin);
                        }

                    }
                    for (int i = EndItemIndex - 1; i >= StartItemIndex; i--)
                    {
                        var viewIndex = i - StartItemIndex;
                        var senkou = ic.Senkou2Data.ValueOf(i);
                        if (senkou.Value == 0)
                        {
                            continue;
                        }
                        if (isFirstSenkou)
                        {
                            isFirstSenkou = false;
                            senkouPath.MoveTo(actualItemFullWidth * (viewIndex + 0.5f), actualHeight * (float)(1.0 - (senkou.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin);
                        }
                        else
                        {
                            senkouPath.LineTo(actualItemFullWidth * (viewIndex + 0.5f), actualHeight * (float)(1.0 - (senkou.Value - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin);
                        }
                    }
                    canvas.DrawPath(senkouPath, new SKPaint()
                    {
                        Style = SKPaintStyle.Fill,
                        Color = ic.Senkou1LineColor.Color.ToSKColor()
                    });
                }
                else // Normal Mode
                {
                    for (int i = StartItemIndex; i < EndItemIndex; i++)
                    {
                        var viewIndex = i - StartItemIndex;
                        if (i < ic.TenkanData.Count && i >= 1)
                        {
                            DrawIndicatorLine(canvas, ic.TenkanData.ValueOf(i - 1), ic.TenkanData.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = ic.TenkanLineColor.Color.ToSKColor(), StrokeWidth = ic.TenkanLineWeight.LineWeight.ToStrokeWidth() });
                        }
                        if (i < ic.KijunData.Count && i >= 1)
                        {
                            DrawIndicatorLine(canvas, ic.KijunData.ValueOf(i - 1), ic.KijunData.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = ic.KijunLineColor.Color.ToSKColor(), StrokeWidth = ic.KijunLineWeight.LineWeight.ToStrokeWidth() });
                        }
                        if (i < ic.ChikouData.Count && i >= 1)
                        {
                            DrawIndicatorLine(canvas, ic.ChikouData.ValueOf(i - 1), ic.ChikouData.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = ic.ChikouLineColor.Color.ToSKColor(), StrokeWidth = ic.ChikouLineWeight.LineWeight.ToStrokeWidth() });
                        }
                        if (i < ic.Senkou1Data.Count && i >= 1)
                        {
                            DrawIndicatorLine(canvas, ic.Senkou1Data.ValueOf(i - 1), ic.Senkou1Data.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = ic.Senkou1LineColor.Color.ToSKColor(), StrokeWidth = ic.Senkou1LineWeight.LineWeight.ToStrokeWidth() });
                        }
                        if (i < ic.Senkou2Data.Count && i >= 1)
                        {
                            DrawIndicatorLine(canvas, ic.Senkou2Data.ValueOf(i - 1), ic.Senkou2Data.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = ic.Senkou2LineColor.Color.ToSKColor(), StrokeWidth = ic.Senkou2LineWeight.LineWeight.ToStrokeWidth() });
                        }
                    }
                }
            }

            // Draw Moving Average, Bollinger Bands, RSI
            for (int i = StartItemIndex; i < EndItemIndex; i++)
            {
                var viewIndex = i - StartItemIndex;

                foreach (var ma in mas)
                {
                    if (!ma.Enable)
                    {
                        continue;
                    }

                    if (i < ma.Data.Count && i >= 1)
                    {
                        DrawIndicatorLine(canvas, ma.Data.ValueOf(i - 1), ma.Data.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = ma.LineColor.Color.ToSKColor(), StrokeWidth = ma.LineWeight.LineWeight.ToStrokeWidth() });
                    }
                }
                foreach (var bb in bbs)
                {
                    if (!bb.Enable)
                    {
                        continue;
                    }

                    if (i < bb.SmaData.Count && i >= 1)
                    {
                        DrawIndicatorLine(canvas, bb.SmaData.ValueOf(i - 1), bb.SmaData.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = bb.SmaLineColor.Color.ToSKColor(), StrokeWidth = bb.SmaLineWeight.LineWeight.ToStrokeWidth() });
                    }
                    if (i < bb.UpperData.Count && i >= 1)
                    {
                        DrawIndicatorLine(canvas, bb.UpperData.ValueOf(i - 1), bb.UpperData.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = bb.UpperLineColor.Color.ToSKColor(), StrokeWidth = bb.UpperLineWeight.LineWeight.ToStrokeWidth() });
                    }
                    if (i < bb.LowerData.Count && i >= 1)
                    {
                        DrawIndicatorLine(canvas, bb.LowerData.ValueOf(i - 1), bb.LowerData.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, yMax, yMin, new SKPaint() { Color = bb.LowerLineColor.Color.ToSKColor(), StrokeWidth = bb.LowerLineWeight.LineWeight.ToStrokeWidth() });
                    }
                }
                if (rsi.Enable)
                {
                    if (i < rsi.Data.Count && i >= 1)
                    {
                        DrawIndicatorLine(canvas, rsi.Data.ValueOf(i - 1), rsi.Data.ValueOf(i), viewIndex, actualItemFullWidth, actualHeight, 100, 0, new SKPaint() { Color = rsi.LineColor.Color.ToSKColor(), StrokeWidth = rsi.LineWeight.LineWeight.ToStrokeWidth() });
                    }
                }
            }

            // Draw Price Candlestick
            for (int i = StartItemIndex; i < EndItemIndex; i++)
            {
                var quote = Quotes[i];
                var viewIndex = i - StartItemIndex;

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - ((double)quote.High - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - ((double)quote.Low - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin),
                    quote.Open < quote.Close ? DrawingTools.LongPaint : DrawingTools.ShortPaint);
                canvas.DrawRect(
                    new SKRect(
                        actualItemFullWidth * viewIndex + actualItemMargin / 2,
                        actualHeight * (float)(1.0 - ((double)quote.Open - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin,
                        actualItemFullWidth * (viewIndex + 1) - actualItemMargin / 2,
                        actualHeight * (float)(1.0 - ((double)quote.Close - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin
                        ),
                    quote.Open < quote.Close ? DrawingTools.LongPaint : DrawingTools.ShortPaint
                    );
            }

            // Draw Candle Pointer
            canvas.DrawRect(
                (int)(CurrentMouseX / actualItemFullWidth) * actualItemFullWidth,
                0,
                actualItemFullWidth,
                (float)CandleChart.ActualHeight,
                DrawingTools.CandlePointerPaint
                );

            // Draw Candle Info Text
            try
            {
                var pointingQuote = CurrentMouseX == -1358 ? Quotes[EndItemIndex - 1] : Quotes[StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth)];
                var changeText = pointingQuote.Close >= pointingQuote.Open ? $"+{(pointingQuote.Close - pointingQuote.Open) / pointingQuote.Open:P2}" : $"{(pointingQuote.Close - pointingQuote.Open) / pointingQuote.Open:P2}";
                var candleInfoText = new List<SKColoredText>
            {
                new SKColoredText($"{pointingQuote.Date.ToLocalTime():yyyy-MM-dd HH:mm:ss}  V", DrawingTools.BaseColor, -5),
                new SKColoredText(NumberUtil.ToRoundedValueString(pointingQuote.Volume), pointingQuote.Open < pointingQuote.Close ? DrawingTools.LongColor : DrawingTools.ShortColor, -4),
                SKColoredText.NewLine,
                new SKColoredText("O", DrawingTools.BaseColor),
                new SKColoredText(NumberUtil.ToRoundedValueString(pointingQuote.Open), pointingQuote.Open < pointingQuote.Close ? DrawingTools.LongColor : DrawingTools.ShortColor, -4),
                 new SKColoredText("H", DrawingTools.BaseColor),
                new SKColoredText(NumberUtil.ToRoundedValueString(pointingQuote.High), pointingQuote.Open < pointingQuote.Close ? DrawingTools.LongColor : DrawingTools.ShortColor, -4),
                 new SKColoredText("L", DrawingTools.BaseColor),
                new SKColoredText(NumberUtil.ToRoundedValueString(pointingQuote.Low), pointingQuote.Open < pointingQuote.Close ? DrawingTools.LongColor : DrawingTools.ShortColor, -4),
                 new SKColoredText("C", DrawingTools.BaseColor),
                new SKColoredText($"{NumberUtil.ToRoundedValueString(pointingQuote.Close)}({changeText})", pointingQuote.Open < pointingQuote.Close ? DrawingTools.LongColor : DrawingTools.ShortColor, -4),
            };
                canvas.DrawColoredText(candleInfoText, 3, 10, DrawingTools.CandleInfoFont, -3);
            }
            catch
            {
            }
            // Draw Indicator Info Text
            try
            {
                var indicatorInfoText = new List<SKColoredText>();
                foreach (var ma in SettingsMan.Indicators.Mas)
                {
                    if (!ma.Enable)
                    {
                        continue;
                    }

                    var pointingIndicator = CurrentMouseX == -1358 ? ma.Data.ValueOf(EndItemIndex - 1) : ma.Data.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));

                    indicatorInfoText.Add(new SKColoredText($"{ma.Type.Type.ToString().ToUpper()} {ma.Period}", DrawingTools.BaseColor));
                    if (pointingIndicator.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicator.Value, significantDigit).ToString(), ma.LineColor.Color.ToSKColor()));
                    }
                    indicatorInfoText.Add(SKColoredText.NewLine);
                }
                foreach (var bb in SettingsMan.Indicators.Bbs)
                {
                    if (!bb.Enable)
                    {
                        continue;
                    }

                    var pointingIndicatorSma = CurrentMouseX == -1358 ? bb.SmaData.ValueOf(EndItemIndex - 1) : bb.SmaData.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));
                    var pointingIndicatorUpper = CurrentMouseX == -1358 ? bb.UpperData.ValueOf(EndItemIndex - 1) : bb.UpperData.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));
                    var pointingIndicatorLower = CurrentMouseX == -1358 ? bb.LowerData.ValueOf(EndItemIndex - 1) : bb.LowerData.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));

                    indicatorInfoText.Add(new SKColoredText($"BB {bb.Period},{bb.Deviation}", DrawingTools.BaseColor));
                    if (pointingIndicatorSma.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorLower.Value, significantDigit).ToString(), bb.LowerLineColor.Color.ToSKColor(), -4));
                    }
                    if (pointingIndicatorUpper.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorSma.Value, significantDigit).ToString(), bb.SmaLineColor.Color.ToSKColor(), -4));
                    }
                    if (pointingIndicatorLower.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorUpper.Value, significantDigit).ToString(), bb.UpperLineColor.Color.ToSKColor(), -4));
                    }
                    indicatorInfoText.Add(SKColoredText.NewLine);
                }
                if (ic.Enable)
                {
                    indicatorInfoText.Add(new SKColoredText($"Ichimoku {ic.ShortPeriod},{ic.MidPeriod},{ic.LongPeriod} ", DrawingTools.BaseColor, -5));
                    if (!ic.CloudEnable)
                    {
                        var pointingIndicatorTenkan = CurrentMouseX == -1358 ? ic.TenkanData.ValueOf(EndItemIndex - 1) : ic.TenkanData.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));
                        var pointingIndicatorKijun = CurrentMouseX == -1358 ? ic.KijunData.ValueOf(EndItemIndex - 1) : ic.KijunData.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));
                        var pointingIndicatorChikou = CurrentMouseX == -1358 ? ic.ChikouData.ValueOf(EndItemIndex - 1) : ic.ChikouData.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));

                        if (pointingIndicatorTenkan.Value != 0)
                        {
                            indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorTenkan.Value, significantDigit).ToString(), ic.TenkanLineColor.Color.ToSKColor(), -4));
                        }
                        if (pointingIndicatorKijun.Value != 0)
                        {
                            indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorKijun.Value, significantDigit).ToString(), ic.KijunLineColor.Color.ToSKColor(), -4));
                        }
                        if (pointingIndicatorChikou.Value != 0)
                        {
                            indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorChikou.Value, significantDigit).ToString(), ic.ChikouLineColor.Color.ToSKColor(), -4));
                        }
                    }

                    var pointingIndicatorSenkou1 = CurrentMouseX == -1358 ? ic.Senkou1Data.ValueOf(EndItemIndex - 1) : ic.Senkou1Data.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));
                    var pointingIndicatorSenkou2 = CurrentMouseX == -1358 ? ic.Senkou2Data.ValueOf(EndItemIndex - 1) : ic.Senkou2Data.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));

                    if (pointingIndicatorSenkou1.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorSenkou1.Value, significantDigit).ToString(), ic.Senkou1LineColor.Color.ToSKColor(), -4));
                    }
                    if (pointingIndicatorSenkou2.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicatorSenkou2.Value, significantDigit).ToString(), ic.Senkou2LineColor.Color.ToSKColor(), -4));
                    }
                    indicatorInfoText.Add(SKColoredText.NewLine);
                }
                if (rsi.Enable)
                {
                    var pointingIndicator = CurrentMouseX == -1358 ? rsi.Data.ValueOf(EndItemIndex - 1) : rsi.Data.ValueOf(StartItemIndex + (int)(CurrentMouseX / actualItemFullWidth));

                    indicatorInfoText.Add(new SKColoredText($"RSI {rsi.Period}", DrawingTools.BaseColor));
                    if (pointingIndicator.Value != 0)
                    {
                        indicatorInfoText.Add(new SKColoredText(Math.Round(pointingIndicator.Value, significantDigit).ToString(), rsi.LineColor.Color.ToSKColor(), -4));
                    }
                    indicatorInfoText.Add(SKColoredText.NewLine);
                }
                canvas.DrawColoredText(indicatorInfoText, 3, 33, DrawingTools.CandleInfoFont, -3);
            }
            catch
            {
            }
        }

        private void CandleChartAxis_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (ViewItemCount <= 1)
            {
                return;
            }

            var actualHeight = (float)CandleChartAxis.ActualHeight - Common.CandleTopBottomMargin * 2;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            (var yMax, var yMin) = GetYMaxMin();
            var significantDigit = NumberUtil.GetSignificantDigitCount(Quotes[^1].Open);

            // Draw Grid
            var gridLevel = 4; // 4등분
            for (int i = 0; i <= gridLevel; i++)
            {
                var gridPriceString = Math.Round(yMin + (yMax - yMin) * ((gridLevel - i) / gridLevel), significantDigit).ToString();

                canvas.DrawText(
                    gridPriceString,
                    5,
                    actualHeight * ((float)i / gridLevel) + Common.CandleTopBottomMargin,
                    DrawingTools.GridTextFont,
                    DrawingTools.GridTextPaint);
            }

            // Draw Current Price Ticker
            canvas.DrawText(
                NumberUtil.ToRoundedValueString(Quotes[EndItemIndex - 1].Close),
                5,
                actualHeight * (float)(1.0 - ((double)Quotes[EndItemIndex - 1].Close - yMin) / (yMax - yMin)) + Common.CandleTopBottomMargin,
                DrawingTools.CurrentTickerFont,
                Quotes[EndItemIndex - 1].Open < Quotes[EndItemIndex - 1].Close ? DrawingTools.LongPaint : DrawingTools.ShortPaint
                );
        }

        private void VolumeChart_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (ViewItemCount <= 1)
            {
                return;
            }

            var actualWidth = (float)VolumeChart.ActualWidth;
            var actualHeight = (float)VolumeChart.ActualHeight - Common.VolumeTopBottomMargin * 2;
            var actualItemFullWidth = actualWidth / ViewItemCount;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var volumeMax = Quotes.Skip(StartItemIndex).Take(ViewItemCount).Max(x => x.Volume);

            // Draw Grid
            var gridLevel = 2; // 2등분
            for (int i = 0; i <= gridLevel; i++)
            {
                canvas.DrawLine(
                    new SKPoint(0, actualHeight * ((float)i / gridLevel) + Common.VolumeTopBottomMargin),
                    new SKPoint(actualWidth, actualHeight * ((float)i / gridLevel) + Common.VolumeTopBottomMargin),
                    DrawingTools.GridPaint
                    );
            }

            // Draw Candle Pointer
            canvas.DrawRect(
                (int)(CurrentMouseX / actualItemFullWidth) * actualItemFullWidth,
                0,
                actualItemFullWidth,
                (float)VolumeChart.ActualHeight,
                DrawingTools.CandlePointerPaint
                );

            for (int i = StartItemIndex; i < EndItemIndex; i++)
            {
                var quote = Quotes[i];
                var viewIndex = i - StartItemIndex;

                // Draw Volume Histogram
                canvas.DrawRect(
                    new SKRect(
                        actualItemFullWidth * viewIndex + ActualItemMargin / 2,
                        actualHeight * (float)(1.0m - quote.Volume / volumeMax) + Common.VolumeTopBottomMargin,
                        actualItemFullWidth * (viewIndex + 1) - ActualItemMargin / 2,
                        actualHeight + Common.VolumeTopBottomMargin
                        ),
                    quote.Open < quote.Close ? DrawingTools.LongPaint : DrawingTools.ShortPaint
                    );
            }
        }

        private void VolumeChartAxis_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (ViewItemCount <= 1)
            {
                return;
            }

            var actualHeight = (float)VolumeChartAxis.ActualHeight - Common.VolumeTopBottomMargin * 2;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var volumeMax = Quotes.Skip(StartItemIndex).Take(ViewItemCount).Max(x => x.Volume);

            // Draw Grid
            var gridLevel = 2; // 2등분
            for (int i = 0; i <= gridLevel; i++)
            {
                var gridPriceString = NumberUtil.ToRoundedValueString(volumeMax * ((decimal)(gridLevel - i) / gridLevel));

                canvas.DrawText(
                    gridPriceString,
                    5,
                    (actualHeight - 6) * ((float)i / gridLevel) + 5 + Common.VolumeTopBottomMargin,
                    DrawingTools.GridTextFont,
                    DrawingTools.GridTextPaint
                    );
            }

            // Draw Current Volume Ticker
            canvas.DrawText(
                NumberUtil.ToRoundedValueString(Quotes[EndItemIndex - 1].Volume),
                5,
                (actualHeight - 6) * (float)(1.0m - Quotes[EndItemIndex - 1].Volume / volumeMax) + 10 + Common.VolumeTopBottomMargin,
                DrawingTools.CurrentTickerFont,
                Quotes[EndItemIndex - 1].Open < Quotes[EndItemIndex - 1].Close ? DrawingTools.LongPaint : DrawingTools.ShortPaint
                );
        }
        #endregion
    }
}
