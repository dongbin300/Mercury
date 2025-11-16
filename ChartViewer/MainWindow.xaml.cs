using Binance.Net.Enums;

using Mercury.Backtests;
using Mercury.Charts;
using Mercury.Charts.Technicals;
using Mercury.Extensions;
using Mercury.Maths;

using Microsoft.Win32;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChartViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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

        private List<decimal> LongPoints = [];
        private List<decimal> ShortPoints = [];

        private const int ERROR_VALUE = -39909;
        private readonly SKFont CandleInfoFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 11);
        private readonly SKPaint CandleInfoPaint = new() { Color = SKColors.White };
        private readonly SKPaint HorizontalLinePointerPaint = new() { Color = SKColors.Silver };
        private readonly SKPaint LongHorizontalLinePointerPaint = new() { Color = new SKColor(59, 207, 134) };
        private readonly SKPaint ShortHorizontalLinePointerPaint = new() { Color = new SKColor(237, 49, 97) };
        private static readonly SKColor LongColor = new(59, 207, 134);
        private static readonly SKColor LongVolumeColor = new(59, 207, 134, 64);
        private static readonly SKColor ShortColor = new(237, 49, 97);
        private static readonly SKColor ShortVolumeColor = new(237, 49, 97, 64);
        private readonly SKPaint LongPaint = new() { Color = LongColor };
        private readonly SKPaint LongVolumePaint = new() { Color = LongVolumeColor };
        private readonly SKPaint ShortPaint = new() { Color = ShortColor };
        private readonly SKPaint ShortVolumePaint = new() { Color = ShortVolumeColor };
        private readonly SKPaint CandlePointerPaint = new() { Color = new SKColor(255, 255, 255, 16) };
        private readonly SKPaint CandleBuyPointerPaint = new() { Color = new SKColor(59, 207, 134, 64) };
        private readonly SKPaint CandleSellPointerPaint = new() { Color = new SKColor(237, 49, 97, 64) };
        private readonly int CandleTopBottomMargin = 10;
        List<ChartInfo> Charts = [];
        private int ChartCount => Charts.Count;
        public float CurrentMouseX;
        public float CurrentMouseY;

        float LiveActualWidth;
        float LiveActualHeight;
        float LiveActualItemFullWidth => LiveActualWidth / ChartCount;
        float LiveActualItemMargin => LiveActualItemFullWidth * 0.2f;

        public MainWindow()
        {
            InitializeComponent();
            SymbolTextBox.Focus();

            /* init */
            SymbolTextBox.Text = Settings.Default.Symbol;
            DateTextBox.Text = Settings.Default.Date;
            CandleCountTextBox.Text = Settings.Default.CandleCount;
            IntervalComboBox.SelectedIndex = Settings.Default.Interval;
            CandleCountTextBox.Focus();

            CciCheckBox.IsChecked = true;
            IcCheckBox.IsChecked = true;
        }

        private void SymbolTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SymbolTextBox.Text.EndsWith("USDT"))
            {
                DateTextBox.Focus();
            }
        }

        private void DateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DateTextBox.Text.Length == 4)
            {
                DateTextBox.AppendText("-");
                DateTextBox.CaretIndex = DateTextBox.Text.Length;
            }
            else if (DateTextBox.Text.Length == 7)
            {
                DateTextBox.AppendText("-");
                DateTextBox.CaretIndex = DateTextBox.Text.Length;
            }
            else if (DateTextBox.Text.Length == 10)
            {
                CandleCountTextBox.Focus();
            }
        }

        private void CandleCountTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Settings.Default.Symbol = SymbolTextBox.Text;
                Settings.Default.Date = DateTextBox.Text;
                Settings.Default.CandleCount = CandleCountTextBox.Text;
                Settings.Default.Interval = IntervalComboBox.SelectedIndex;
                Settings.Default.Save();
                LoadChart();
            }
        }

        void LoadChart()
        {
            var symbol = SymbolTextBox.Text;
            var interval = (IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToKlineInterval() ?? KlineInterval.FiveMinutes;
            var candleCount = CandleCountTextBox.Text.ToInt();
            var startDate = DateTextBox.Text.ToDateTime();
            var endDate = startDate.AddSeconds((int)interval * candleCount);
            ChartLoader.Charts.Clear();
            ChartLoader.InitCharts(symbol, interval, startDate, endDate);
            Charts = [.. ChartLoader.GetChartPack(symbol, interval).Charts];

            // Calculate Indicators
            var quotes = Charts.Select(x => x.Quote);
            if (Ma1CheckBox.IsChecked ?? true)
            {
                var ma = quotes.GetSma(Ma1Text.Text.ToInt()).Select(x => x.Sma);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var smaValue = ma.ElementAt(i);
                    Charts[i].Sma1 = smaValue.HasValue ? smaValue.Value : ERROR_VALUE;
                }
            }
            if (Ma2CheckBox.IsChecked ?? true)
            {
                var ma = quotes.GetSma(Ma2Text.Text.ToInt()).Select(x => x.Sma);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var smaValue = ma.ElementAt(i);
                    Charts[i].Sma2 = smaValue.HasValue ? smaValue.Value : ERROR_VALUE;
                }
            }
            if (Ma3CheckBox.IsChecked ?? true)
            {
                var ma = quotes.GetSma(Ma3Text.Text.ToInt()).Select(x => x.Sma);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var smaValue = ma.ElementAt(i);
                    Charts[i].Sma3 = smaValue.HasValue ? smaValue.Value : ERROR_VALUE;
                }
            }
            if (Ema1CheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(Ema1Text.Text.ToInt()).Select(x => x.Ema);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var emaValue = ema.ElementAt(i);
                    Charts[i].Ema1 = emaValue.HasValue ? emaValue.Value : ERROR_VALUE;
                }
            }
            if (Ema2CheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(Ema2Text.Text.ToInt()).Select(x => x.Ema);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var emaValue = ema.ElementAt(i);
                    Charts[i].Ema2 = emaValue.HasValue ? emaValue.Value : ERROR_VALUE;
                }
            }
            if (Ema3CheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(Ema3Text.Text.ToInt()).Select(x => x.Ema);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var emaValue = ema.ElementAt(i);
                    Charts[i].Ema3 = emaValue.HasValue ? emaValue.Value : ERROR_VALUE;
                }
            }
            if (Supertrend1CheckBox.IsChecked ?? true)
            {
                var st = quotes.GetSupertrend(Supertrend1PeriodText.Text.ToInt(), Supertrend1FactorText.Text.ToDouble()).Select(x => x.Supertrend);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var stValue = st.ElementAt(i);
                    Charts[i].Supertrend1 = stValue.HasValue ? stValue.Value : ERROR_VALUE;
                }
            }
            if (Supertrend2CheckBox.IsChecked ?? true)
            {
                var st = quotes.GetSupertrend(Supertrend2PeriodText.Text.ToInt(), Supertrend2FactorText.Text.ToDouble()).Select(x => x.Supertrend);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var stValue = st.ElementAt(i);
                    Charts[i].Supertrend2 = stValue.HasValue ? stValue.Value : ERROR_VALUE;
                }
            }
            if (Supertrend3CheckBox.IsChecked ?? true)
            {
                var st = quotes.GetSupertrend(Supertrend3PeriodText.Text.ToInt(), Supertrend3FactorText.Text.ToDouble()).Select(x => x.Supertrend);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var stValue = st.ElementAt(i);
                    Charts[i].Supertrend3 = stValue.HasValue ? stValue.Value : ERROR_VALUE;
                }
            }
            if (RSupertrend1CheckBox.IsChecked ?? true)
            {
                var st = quotes.GetReverseSupertrend(RSupertrend1PeriodText.Text.ToInt(), RSupertrend1FactorText.Text.ToDouble()).Select(x => x.Supertrend);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var stValue = st.ElementAt(i);
                    Charts[i].ReverseSupertrend1 = stValue.HasValue ? stValue.Value : ERROR_VALUE;
                }
            }
            if (CustomCheckBox.IsChecked ?? true)
            {
                var custom = quotes.GetCustom(CustomPeriodText.Text.ToInt());
                var upper = custom.Select(x => x.Upper);
                var lower = custom.Select(x => x.Lower);
                var pioneer = custom.Select(x => x.Pioneer);
                var player = custom.Select(x => x.Player);
                for (int i = 0; i < Charts.Count; i++)
                {
                    Charts[i].CustomUpper = upper.ElementAt(i) == 0 ? ERROR_VALUE : upper.ElementAt(i);
                    Charts[i].CustomLower = lower.ElementAt(i) == 0 ? ERROR_VALUE : lower.ElementAt(i);
                    Charts[i].CustomPioneer = pioneer.ElementAt(i) == 0 ? ERROR_VALUE : pioneer.ElementAt(i);
                    Charts[i].CustomPlayer = player.ElementAt(i) == 0 ? ERROR_VALUE : player.ElementAt(i);
                }
            }
            if (TrendLineCheckBox.IsChecked ?? true)
            {
                var checkpoints = new Checkpoints();
                checkpoints.EvaluateCheckpoint(Charts, TrendLinePeriodText.Text.ToInt());
                for (int i = 0; i < Charts.Count; i++)
                {
                    var highPoint = checkpoints.HighPoints.Where(x => x.Time.Equals(Charts[i].DateTime));
                    Charts[i].TrendLineUpper = highPoint.Any() ? highPoint.First().Price : ERROR_VALUE;

                    var lowPoint = checkpoints.LowPoints.Where(x => x.Time.Equals(Charts[i].DateTime));
                    Charts[i].TrendLineLower = lowPoint.Any() ? lowPoint.First().Price : ERROR_VALUE;
                }

                FillTrendLineValue();
            }
            if (TrendRiderCheckBox.IsChecked ?? true)
            {
                var trendRider = quotes.GetTrendRider(25, 2.5, 25, 12, 26, 9);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var trendRiderItem = trendRider.ElementAt(i);
                    Charts[i].TrendRiderTrend = trendRiderItem.Trend;
                    Charts[i].TrendRiderSupertrend = trendRiderItem.Supertrend.HasValue ? trendRiderItem.Supertrend.Value : ERROR_VALUE;
                }
            }
            if (EmaAtrCheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(EmaAtrEmaPeriodText.Text.ToInt());
                var atr = quotes.GetAtr(EmaAtrAtrPeriodText.Text.ToInt());
                for (int i = 0; i < Charts.Count; i++)
                {
                    var emaValue = ema.ElementAt(i).Ema;
                    var atrValue = atr.ElementAt(i).Atr;

                    if (!emaValue.HasValue || !atrValue.HasValue)
                    {
                        Charts[i].EmaAtrLower = ERROR_VALUE;
                        Charts[i].EmaAtrUpper = ERROR_VALUE;
                        continue;
                    }

                    Charts[i].EmaAtrLower = emaValue.Value - atrValue.Value;
                    Charts[i].EmaAtrUpper = emaValue.Value + atrValue.Value;
                }
            }
            if (RsiCheckBox.IsChecked ?? true)
            {
                var rsi = quotes.GetRsi(RsiPeriodText.Text.ToInt()).Select(x => x.Rsi);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var rsiValue = rsi.ElementAt(i);
                    Charts[i].Rsi1 = rsiValue.HasValue ? rsiValue.Value : ERROR_VALUE;
                }
            }
            if (AtrCheckBox.IsChecked ?? true)
            {
                var atr = quotes.GetAtr(AtrPeriodText.Text.ToInt()).Select(x => x.Atr);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var atrValue = atr.ElementAt(i);
                    Charts[i].Atr = atrValue.HasValue ? atrValue.Value : ERROR_VALUE;
                }
            }
            if (BbCheckBox.IsChecked ?? true)
            {
                var bb = quotes.GetBollingerBands(BbPeriodText.Text.ToInt(), BbDeviationText.Text.ToDouble());
                for (int i = 0; i < Charts.Count; i++)
                {
                    var bbItem = bb.ElementAt(i);
                    Charts[i].Bb1Sma = bbItem.Sma.HasValue ? bbItem.Sma.Value : ERROR_VALUE;
                    Charts[i].Bb1Upper = bbItem.Upper.HasValue ? bbItem.Upper.Value : ERROR_VALUE;
                    Charts[i].Bb1Lower = bbItem.Lower.HasValue ? bbItem.Lower.Value : ERROR_VALUE;
                }
            }

            if (CciCheckBox.IsChecked ?? true)
            {
                var cci = quotes.GetCci(CciPeriodText.Text.ToInt()).Select(x => x.Cci);
                for (int i = 0; i < Charts.Count; i++)
                {
                    var cciValue = cci.ElementAt(i);
                    Charts[i].Cci = cciValue.HasValue ? cciValue.Value : ERROR_VALUE;
                }
            }

            if (VolatilityCheckBox.IsChecked ?? true)
            {
                var period = VolatilityPeriodText.Text.ToInt();
                for (int i = 0; i < Charts.Count; i++)
                {
                    if (i < period - 1) continue;
                    var recent = Charts.Skip(i - period + 1).Take(period).ToList();
                    var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
                    Charts[i].Volatility = avgRange;
                }
            }

            if (TrendStrengthCheckBox.IsChecked ?? true)
            {
                var period = TrendStrengthPeriodText.Text.ToInt();
                for (int i = 0; i < Charts.Count; i++)
                {
                    if (i < period - 1) continue;
                    var recent = Charts.Skip(i - period + 1).Take(period).ToList();
                    var firstPrice = recent.First().Quote.Close;
                    var lastPrice = recent.Last().Quote.Close;
                    Charts[i].TrendStrength = (lastPrice - firstPrice) / firstPrice * 100;
                }
            }

			if (IcCheckBox.IsChecked ?? true)
			{
                var ic = quotes.GetIchimokuCloud(IcConversionText.Text.ToInt(), IcBaseText.Text.ToInt(), IcLeadingSpanText.Text.ToInt());
                var conversion = ic.Select(x => x.Conversion);
                var _base = ic.Select(x => x.Base);
                var trailingSpan = ic.Select(x => x.TrailingSpan);
                var leadingSpan1 = ic.Select(x => x.LeadingSpan1);
                var leadingSpan2 = ic.Select(x => x.LeadingSpan2);
				for (int i = 0; i < Charts.Count; i++)
				{
                    var _conversion = conversion.ElementAt(i);
                    var __base = _base.ElementAt(i);
                    var _trailingSpan = trailingSpan.ElementAt(i);
                    var _leadingSpan1 = leadingSpan1.ElementAt(i);
                    var _leadingSpan2 = leadingSpan2.ElementAt(i);

					Charts[i].IcConversion = _conversion.HasValue ? _conversion.Value : ERROR_VALUE;
                    Charts[i].IcBase = __base.HasValue ? __base.Value : ERROR_VALUE;
                    Charts[i].IcTrailingSpan = _trailingSpan.HasValue ? _trailingSpan.Value : ERROR_VALUE;
                    Charts[i].IcLeadingSpan1 = _leadingSpan1.HasValue ? _leadingSpan1.Value : ERROR_VALUE;
                    Charts[i].IcLeadingSpan2 = _leadingSpan2.HasValue ? _leadingSpan2.Value : ERROR_VALUE;
				}
			}

			CandleChart.InvalidateVisual();
        }

        void FillTrendLineValue()
        {
            int startIndex = -1;
            decimal? startValue = -1;
            for (int i = 0; i < Charts.Count; i++)
            {
                if (Charts[i].TrendLineUpper != ERROR_VALUE)
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                        startValue = Charts[i].TrendLineUpper;
                    }
                    else
                    {
                        int endIndex = i;
                        var endValue = Charts[i].TrendLineUpper;
                        var distance = endIndex - startIndex + 1;
                        var diff = endValue - startValue;
                        for (int j = startIndex + 1; j < endIndex; j++)
                        {
                            var value = startValue + (diff * (j - startIndex) / distance);
                            Charts[j].TrendLineUpper = value;
                        }

                        startIndex = endIndex;
                        startValue = endValue;
                    }
                }
            }

            for (int i = 0; i < Charts.Count; i++)
            {
                if (Charts[i].TrendLineLower != ERROR_VALUE)
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                        startValue = Charts[i].TrendLineLower;
                    }
                    else
                    {
                        int endIndex = i;
                        var endValue = Charts[i].TrendLineLower;
                        var distance = endIndex - startIndex + 1;
                        var diff = endValue - startValue;
                        for (int j = startIndex + 1; j < endIndex; j++)
                        {
                            var value = startValue + (diff * (j - startIndex) / distance);
                            Charts[j].TrendLineLower = value;
                        }

                        startIndex = endIndex;
                        startValue = endValue;
                    }
                }
            }
        }

        private void DrawIndicator(SKCanvas canvas, int viewIndex, decimal? preValue, decimal? value, decimal max, decimal min, SKColor color, float strokeWidth = 1)
        {
            if (preValue == null || value == null || preValue == ERROR_VALUE || value == ERROR_VALUE)
            {
                return;
            }

            canvas.DrawLine(
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex - 0.5f),
                        LiveActualHeight * (float)(1.0m - (preValue - min) / (max - min)) + CandleTopBottomMargin),
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex + 0.5f),
                        LiveActualHeight * (float)(1.0m - (value - min) / (max - min)) + CandleTopBottomMargin),
                    new SKPaint() { Color = color, StrokeWidth = strokeWidth }
                    );
        }

        private void DrawSubIndicator(SKCanvas canvas, int viewIndex, decimal? preValue, decimal? value, decimal max, decimal min, SKColor color, float strokeWidth = 1)
        {
            if (preValue == null || value == null || preValue == ERROR_VALUE || value == ERROR_VALUE)
            {
                return;
            }

            var y_start = LiveActualHeight * 0.66f;
            var y_height = LiveActualHeight * 0.33f;

            canvas.DrawLine(
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex - 0.5f),
                        y_start + y_height * (float)(1.0m - (preValue - min) / (max - min)) + CandleTopBottomMargin
                        ),
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex + 0.5f),
                        y_start + y_height * (float)(1.0m - (value - min) / (max - min)) + CandleTopBottomMargin
                        ),
                    new SKPaint() { Color = color, StrokeWidth = strokeWidth }
                    );
        }

        private void DrawSupertrend(SKCanvas canvas, int viewIndex, decimal? preValue, decimal? value, decimal max, decimal min, SKColor color)
        {
            if (preValue == ERROR_VALUE || value == ERROR_VALUE || (preValue < 0 && value >= 0) || (preValue >= 0 && value < 0))
            {
                return;
            }

            canvas.DrawLine(
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex - 0.5f),
                        LiveActualHeight * (float)(1.0m - (Math.Abs(preValue ?? 0) - min) / (max - min)) + CandleTopBottomMargin),
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex + 0.5f),
                        LiveActualHeight * (float)(1.0m - (Math.Abs(value ?? 0) - min) / (max - min)) + CandleTopBottomMargin),
                    new SKPaint() { Color = color }
                    );
        }

        private void CandleChart_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (ChartCount <= 0)
            {
                return;
            }

            LiveActualWidth = (float)CandleChart.ActualWidth;
            LiveActualHeight = (float)CandleChart.ActualHeight - CandleTopBottomMargin * 2;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var yMax = Charts.Max(x => x.Quote.High);
            var yMin = Charts.Min(x => x.Quote.Low);
            var vMax = Charts.Max(x => x.Quote.Volume);
            var vMin = Charts.Min(x => x.Quote.Volume);

            if (Ma1CheckBox.IsChecked ?? true)
            {
                var validSma1 = Charts.Where(x => x.Sma1.HasValue && x.Sma1 != ERROR_VALUE).Select(x => x.Sma1.Value);
                if (validSma1.Any())
                {
                    yMax = Math.Max(yMax, validSma1.Max());
                    yMin = Math.Min(yMin, validSma1.Min());
                }
            }
            if (Ma2CheckBox.IsChecked ?? true)
            {
                var validSma2 = Charts.Where(x => x.Sma2.HasValue && x.Sma2 != ERROR_VALUE).Select(x => x.Sma2.Value);
                if (validSma2.Any())
                {
                    yMax = Math.Max(yMax, validSma2.Max());
                    yMin = Math.Min(yMin, validSma2.Min());
                }
            }
            if (Ma3CheckBox.IsChecked ?? true)
            {
                var validSma3 = Charts.Where(x => x.Sma3.HasValue && x.Sma3 != ERROR_VALUE).Select(x => x.Sma3.Value);
                if (validSma3.Any())
                {
                    yMax = Math.Max(yMax, validSma3.Max());
                    yMin = Math.Min(yMin, validSma3.Min());
                }
            }
            if (Ema1CheckBox.IsChecked ?? true)
            {
                var validEma1 = Charts.Where(x => x.Ema1.HasValue && x.Ema1 != ERROR_VALUE).Select(x => x.Ema1.Value);
                if (validEma1.Any())
                {
                    yMax = Math.Max(yMax, validEma1.Max());
                    yMin = Math.Min(yMin, validEma1.Min());
                }
            }
            if (Ema2CheckBox.IsChecked ?? true)
            {
                var validEma2 = Charts.Where(x => x.Ema2.HasValue && x.Ema2 != ERROR_VALUE).Select(x => x.Ema2.Value);
                if (validEma2.Any())
                {
                    yMax = Math.Max(yMax, validEma2.Max());
                    yMin = Math.Min(yMin, validEma2.Min());
                }
            }
            if (Ema3CheckBox.IsChecked ?? true)
            {
                var validEma3 = Charts.Where(x => x.Ema3.HasValue && x.Ema3 != ERROR_VALUE).Select(x => x.Ema3.Value);
                if (validEma3.Any())
                {
                    yMax = Math.Max(yMax, validEma3.Max());
                    yMin = Math.Min(yMin, validEma3.Min());
                }
            }
            if (Supertrend1CheckBox.IsChecked ?? true)
            {
                var validSt1 = Charts.Where(x => x.Supertrend1.HasValue && x.Supertrend1 != ERROR_VALUE).Select(x => Math.Abs(x.Supertrend1.Value));
                if (validSt1.Any())
                {
                    yMax = Math.Max(yMax, validSt1.Max());
                    yMin = Math.Min(yMin, validSt1.Min());
                }
            }
            if (Supertrend2CheckBox.IsChecked ?? true)
            {
                var validSt2 = Charts.Where(x => x.Supertrend2.HasValue && x.Supertrend2 != ERROR_VALUE).Select(x => Math.Abs(x.Supertrend2.Value));
                if (validSt2.Any())
                {
                    yMax = Math.Max(yMax, validSt2.Max());
                    yMin = Math.Min(yMin, validSt2.Min());
                }
            }
            if (Supertrend3CheckBox.IsChecked ?? true)
            {
                var validSt3 = Charts.Where(x => x.Supertrend3.HasValue && x.Supertrend3 != ERROR_VALUE).Select(x => Math.Abs(x.Supertrend3.Value));
                if (validSt3.Any())
                {
                    yMax = Math.Max(yMax, validSt3.Max());
                    yMin = Math.Min(yMin, validSt3.Min());
                }
            }
            if (RSupertrend1CheckBox.IsChecked ?? true)
            {
                var validRst1 = Charts.Where(x => x.ReverseSupertrend1.HasValue && x.ReverseSupertrend1 != ERROR_VALUE).Select(x => Math.Abs(x.ReverseSupertrend1.Value));
                if (validRst1.Any())
                {
                    yMax = Math.Max(yMax, validRst1.Max());
                    yMin = Math.Min(yMin, validRst1.Min());
                }
            }
            if (CustomCheckBox.IsChecked ?? true)
            {
                // 아직은 필요없음
            }
            if (TrendRiderCheckBox.IsChecked ?? true)
            {
                var validTrSt = Charts.Where(x => x.TrendRiderSupertrend.HasValue && x.TrendRiderSupertrend != ERROR_VALUE).Select(x => Math.Abs(x.TrendRiderSupertrend.Value));
                if (validTrSt.Any())
                {
                    yMax = Math.Max(yMax, validTrSt.Max());
                    yMin = Math.Min(yMin, validTrSt.Min());
                }
            }
            if (EmaAtrCheckBox.IsChecked ?? true)
            {
                var validEmaAtrUpper = Charts.Where(x => x.EmaAtrUpper.HasValue && x.EmaAtrUpper != ERROR_VALUE).Select(x => x.EmaAtrUpper.Value);
                var validEmaAtrLower = Charts.Where(x => x.EmaAtrLower.HasValue && x.EmaAtrLower != ERROR_VALUE).Select(x => x.EmaAtrLower.Value);
                if (validEmaAtrUpper.Any())
                {
                    yMax = Math.Max(yMax, validEmaAtrUpper.Max());
                }
                if (validEmaAtrLower.Any())
                {
                    yMin = Math.Min(yMin, validEmaAtrLower.Min());
                }
            }

            // Draw Quote and Indicator
            for (int i = 0; i < Charts.Count; i++)
            {
                var quote = Charts[i].Quote;

                #region Volatility
                if (VolatilityCheckBox.IsChecked ?? true)
                {
                    if (Charts[i].Volatility > 3.0m)
                    {
                        canvas.DrawRect(
                            new SKRect(
                                LiveActualItemFullWidth * i,
                                0,
                                LiveActualItemFullWidth * (i + 1),
                                LiveActualHeight + CandleTopBottomMargin * 2
                                ),
                            new SKPaint() { Color = new SKColor(255, 255, 0, 20) }
                            );
                    }
                }
                #endregion

                #region Volume
                canvas.DrawRect(
                    new SKRect(
                        LiveActualItemFullWidth * i + LiveActualItemMargin / 2,
                        LiveActualHeight * 0.66f + (float)((decimal)LiveActualHeight * 0.33m * (vMax - quote.Volume) / vMax) + CandleTopBottomMargin,
                        LiveActualItemFullWidth * (i + 1) - LiveActualItemMargin / 2,
                        LiveActualHeight + CandleTopBottomMargin
                        ),
                    quote.Open < quote.Close ? LongVolumePaint : ShortVolumePaint
                    );
                #endregion

                #region Candle
                canvas.DrawLine(
                    new SKPoint(
                        LiveActualItemFullWidth * (i + 0.5f),
                        LiveActualHeight * (float)(1.0m - (quote.High - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        LiveActualItemFullWidth * (i + 0.5f),
                        LiveActualHeight * (float)(1.0m - (quote.Low - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    quote.Open < quote.Close ? LongPaint : ShortPaint);
                canvas.DrawRect(
                    new SKRect(
                        LiveActualItemFullWidth * i + LiveActualItemMargin / 2,
                        LiveActualHeight * (float)(1.0m - (quote.Open - yMin) / (yMax - yMin)) + CandleTopBottomMargin,
                        LiveActualItemFullWidth * (i + 1) - LiveActualItemMargin / 2,
                        LiveActualHeight * (float)(1.0m - (quote.Close - yMin) / (yMax - yMin)) + CandleTopBottomMargin
                        ),
                    quote.Open < quote.Close ? LongPaint : ShortPaint
                    );
                #endregion

                #region Indicator
                if (Ma1CheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Sma1 : Charts[i - 1].Sma1, Charts[i].Sma1, yMax, yMin, new SKColor(128, 128, 128));
                }
                if (Ma2CheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Sma2 : Charts[i - 1].Sma2, Charts[i].Sma2, yMax, yMin, new SKColor(128, 128, 160));
                }
                if (Ma3CheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Sma3 : Charts[i - 1].Sma3, Charts[i].Sma3, yMax, yMin, new SKColor(128, 128, 192));
                }
                if (Ema1CheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Ema1 : Charts[i - 1].Ema1, Charts[i].Ema1, yMax, yMin, new SKColor(128, 128, 128));
                }
                if (Ema2CheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Ema2 : Charts[i - 1].Ema2, Charts[i].Ema2, yMax, yMin, new SKColor(128, 128, 160));
                }
                if (Ema3CheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Ema3 : Charts[i - 1].Ema3, Charts[i].Ema3, yMax, yMin, new SKColor(128, 128, 192));
                }
                if (Supertrend1CheckBox.IsChecked ?? true)
                {
                    DrawSupertrend(canvas, i, i == 0 ? Charts[i].Supertrend1 : Charts[i - 1].Supertrend1, Charts[i].Supertrend1, yMax, yMin, Charts[i].Supertrend1 > 0 ? LongColor : ShortColor);
                }
                if (Supertrend2CheckBox.IsChecked ?? true)
                {
                    DrawSupertrend(canvas, i, i == 0 ? Charts[i].Supertrend2 : Charts[i - 1].Supertrend2, Charts[i].Supertrend2, yMax, yMin, Charts[i].Supertrend2 > 0 ? LongColor : ShortColor);
                }
                if (Supertrend3CheckBox.IsChecked ?? true)
                {
                    DrawSupertrend(canvas, i, i == 0 ? Charts[i].Supertrend3 : Charts[i - 1].Supertrend3, Charts[i].Supertrend3, yMax, yMin, Charts[i].Supertrend3 > 0 ? LongColor : ShortColor);
                }
                if (RSupertrend1CheckBox.IsChecked ?? true)
                {
                    DrawSupertrend(canvas, i, i == 0 ? Charts[i].ReverseSupertrend1 : Charts[i - 1].ReverseSupertrend1, Charts[i].ReverseSupertrend1, yMax, yMin, Charts[i].ReverseSupertrend1 > 0 ? LongColor : ShortColor);
                }
                if (CustomCheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].CustomUpper : Charts[i - 1].CustomUpper, Charts[i].CustomUpper, yMax, yMin, new SKColor(0, 255, 0), 2);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].CustomLower : Charts[i - 1].CustomLower, Charts[i].CustomLower, yMax, yMin, new SKColor(255, 0, 0), 2);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].CustomPioneer : Charts[i - 1].CustomPioneer, Charts[i].CustomPioneer, yMax, yMin, new SKColor(255, 128, 255), 2);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].CustomPlayer : Charts[i - 1].CustomPlayer, Charts[i].CustomPlayer, yMax, yMin, new SKColor(128, 255, 0), 2);
                }
                if (TrendLineCheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].TrendLineUpper : Charts[i - 1].TrendLineUpper, Charts[i].TrendLineUpper, yMax, yMin, new SKColor(41, 98, 255), 2);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].TrendLineLower : Charts[i - 1].TrendLineLower, Charts[i].TrendLineLower, yMax, yMin, new SKColor(41, 98, 255), 2);
                }
                if (TrendRiderCheckBox.IsChecked ?? true)
                {
                    if (Charts[i].TrendRiderTrend != 0)
                    {
                        canvas.DrawRect(
                        LiveActualItemFullWidth * i,
                        0,
                        LiveActualItemFullWidth,
                        (float)CandleChart.ActualHeight,
                        Charts[i].TrendRiderTrend == 1 ? CandleBuyPointerPaint : CandleSellPointerPaint
                        );
                    }

                    DrawSupertrend(canvas, i, i == 0 ? Charts[i].TrendRiderSupertrend : Charts[i - 1].TrendRiderSupertrend, Charts[i].TrendRiderSupertrend, yMax, yMin, Charts[i].TrendRiderSupertrend > 0 ? LongColor : ShortColor);
                }
                if (EmaAtrCheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].EmaAtrUpper : Charts[i - 1].EmaAtrUpper, Charts[i].EmaAtrUpper, yMax, yMin, SKColors.White);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].EmaAtrLower : Charts[i - 1].EmaAtrLower, Charts[i].EmaAtrLower, yMax, yMin, SKColors.White);
                }
                if (RsiCheckBox.IsChecked ?? true)
                {
                    DrawSubIndicator(canvas, i, i == 0 ? Charts[i].Rsi1 : Charts[i - 1].Rsi1, Charts[i].Rsi1, 100, 0, SKColors.Yellow);
                }
                if (CciCheckBox.IsChecked ?? true)
                {
                    var cciList = Charts.Where(x => x.Cci != null && x.Cci != ERROR_VALUE).Select(x => x.Cci.Value);
                    if (cciList.Any())
                    {
                        var cciMin = cciList.Min();
                        var cciMax = cciList.Max();
                        DrawSubIndicator(canvas, i, i == 0 ? Charts[i].Cci : Charts[i - 1].Cci, Charts[i].Cci, cciMax, cciMin, SKColors.White);
                    }
                }
                if (TrendStrengthCheckBox.IsChecked ?? true)
                {
                    var tsList = Charts.Where(x => x.TrendStrength != null && x.TrendStrength != ERROR_VALUE).Select(x => x.TrendStrength.Value);
                    if (tsList.Any())
                    {
                        var tsMin = tsList.Min();
                        var tsMax = tsList.Max();
                        DrawSubIndicator(canvas, i, i == 0 ? Charts[i].TrendStrength : Charts[i - 1].TrendStrength, Charts[i].TrendStrength, tsMax, tsMin, SKColors.Magenta);
                    }
                }
                if (AtrCheckBox.IsChecked ?? true)
                {
                    var atrList = Charts.Where(x => x.Atr != null && x.Atr != ERROR_VALUE).Select(x => x.Atr.Value);
                    if (atrList.Any())
                    {
                        var atrMin = atrList.Min();
                        var atrMax = atrList.Max();
                        DrawSubIndicator(canvas, i, i == 0 ? Charts[i].Atr : Charts[i - 1].Atr, Charts[i].Atr, atrMax, atrMin, SKColors.White);
                    }
                }
                if (BbCheckBox.IsChecked ?? true)
                {
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Bb1Upper : Charts[i - 1].Bb1Upper, Charts[i].Bb1Upper, yMax, yMin, SKColors.Green);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Bb1Sma : Charts[i - 1].Bb1Sma, Charts[i].Bb1Sma, yMax, yMin, SKColors.AliceBlue);
                    DrawIndicator(canvas, i, i == 0 ? Charts[i].Bb1Lower : Charts[i - 1].Bb1Lower, Charts[i].Bb1Lower, yMax, yMin, SKColors.Red);
                }
				if (IcCheckBox.IsChecked ?? true)
				{
					var ic = Charts.Where(x => x.IcBase != null && x.IcBase != ERROR_VALUE);
                    var conversion = ic.Select(x => x.IcConversion.Value);
                    var _base = ic.Select(x => x.IcBase.Value);
                    var trailingSpan = ic.Select(x => x.IcTrailingSpan.Value);
                    var leadingSpan1 = ic.Select(x => x.IcLeadingSpan1.Value);
                    var leadingSpan2 = ic.Select(x => x.IcLeadingSpan2.Value);
					if (_base.Any())
					{
						var icMin = _base.Min();
						var icMax = _base.Max();
						DrawIndicator(canvas, i, i == 0 ? Charts[i].IcConversion : Charts[i - 1].IcConversion, Charts[i].IcConversion, icMax, icMin, SKColors.Orange);
						DrawIndicator(canvas, i, i == 0 ? Charts[i].IcBase : Charts[i - 1].IcBase, Charts[i].IcBase, icMax, icMin, SKColors.Gray);
						DrawIndicator(canvas, i, i == 0 ? Charts[i].IcTrailingSpan : Charts[i - 1].IcTrailingSpan, Charts[i].IcTrailingSpan, icMax, icMin, SKColors.Yellow);
						DrawIndicator(canvas, i, i == 0 ? Charts[i].IcLeadingSpan1 : Charts[i - 1].IcLeadingSpan1, Charts[i].IcLeadingSpan1, icMax, icMin, SKColors.Cyan);
						DrawIndicator(canvas, i, i == 0 ? Charts[i].IcLeadingSpan2 : Charts[i - 1].IcLeadingSpan2, Charts[i].IcLeadingSpan2, icMax, icMin, SKColors.Cyan);
					}
				}
				#endregion
			}

            // Draw Pointer
            canvas.DrawRect(
                (int)(CurrentMouseX / LiveActualItemFullWidth) * LiveActualItemFullWidth,
                0,
                LiveActualItemFullWidth,
                (float)CandleChart.ActualHeight,
                CandlePointerPaint
                );

            // Draw Buy/Sell Pointer
            if (buyIndex != -1 && sellIndex != -1)
            {
                canvas.DrawRect(
                   buyIndex * LiveActualItemFullWidth,
                   0,
                   LiveActualItemFullWidth,
                   (float)CandleChart.ActualHeight,
                   CandleBuyPointerPaint
                   );

                canvas.DrawRect(
                   sellIndex * LiveActualItemFullWidth,
                   0,
                   LiveActualItemFullWidth,
                   (float)CandleChart.ActualHeight,
                   CandleSellPointerPaint
                   );

                canvas.DrawText(resultString, 3, 20, CandleInfoFont, CandleInfoPaint);
            }


            // Draw Horizontal Line Pointer
            canvas.DrawLine(
                0, CurrentMouseY, (float)CandleChart.ActualWidth, CurrentMouseY, HorizontalLinePointerPaint
                );
            // Draw Horizontal Line Price
            var pointingPrice = (((decimal)(CandleTopBottomMargin - CurrentMouseY) / (decimal)LiveActualHeight + 1) * (yMax - yMin) + yMin).Round(4);
            canvas.DrawText($"{pointingPrice}", 2, CurrentMouseY - 4, CandleInfoFont, CandleInfoPaint);

            // Draw Info Text
            try
            {
                CandleInfoFont.Size = Math.Max(10, (float)ActualHeight / 75);
                var pointingChart = CurrentMouseX == -1358 ? Charts[ChartCount - 1] : Charts[(int)(CurrentMouseX / LiveActualItemFullWidth)];
                var changeText = pointingChart.Quote.Close >= pointingChart.Quote.Open ? $"+{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}" : $"{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}";
                var text = $"{pointingChart.DateTime:yyyy-MM-dd HH:mm:ss}, O {pointingChart.Quote.Open} H {pointingChart.Quote.High} L {pointingChart.Quote.Low} C {pointingChart.Quote.Close} ({changeText}) V {pointingChart.Quote.Volume}";

                text += Environment.NewLine;
                if (RsiCheckBox.IsChecked ?? true && pointingChart.Rsi1.HasValue)
                {
                    text += $"RSI {pointingChart.Rsi1.Value.Round(2)} ";
                }
                if (AtrCheckBox.IsChecked ?? true && pointingChart.Atr.HasValue)
                {
                    text += $"ATR {pointingChart.Atr.Value.Round(4)} ";
                }
                if (BbCheckBox.IsChecked ?? true && pointingChart.Bb1Sma.HasValue)
                {
                    text += $"BB-SMA {pointingChart.Bb1Sma.Value.Round(4)} ";
                }

                text += Environment.NewLine;
                if (CciCheckBox.IsChecked ?? true && pointingChart.Cci.HasValue)
                {
                    text += $"CCI {pointingChart.Cci.Value.Round(2)} ";
                }
                if (VolatilityCheckBox.IsChecked ?? true && pointingChart.Volatility.HasValue)
                {
                    text += $"Volatility {pointingChart.Volatility.Value.Round(2)} ";
                }
                if (TrendStrengthCheckBox.IsChecked ?? true && pointingChart.TrendStrength.HasValue)
                {
                    text += $"Trend {pointingChart.TrendStrength.Value.Round(2)} ";
                }
                if(IcCheckBox.IsChecked ?? true && pointingChart.IcBase.HasValue)
                {
                    text += $"IC-Conversion {pointingChart.IcConversion.Value.Round(4)} IC-Base {pointingChart.IcBase.Value.Round(4)} IC-TrailingSpan {pointingChart.IcTrailingSpan.Value.Round(4)} IC-LeadingSpan1 {pointingChart.IcLeadingSpan1.Value.Round(4)} IC-LeadingSpan2 {pointingChart.IcLeadingSpan2.Value.Round(4)} ";
				}

                canvas.DrawText(text, 2, CandleInfoFont.Size + 2, CandleInfoFont, CandleInfoPaint);
            }
            catch
            {
            }

            // Draw Long/Short Points
            foreach (var point in LongPoints)
            {
                var changeText = ShortPoints.Count > 0 ? $"({Calculator.Roe(PositionSide.Short, ShortPoints[0], point) / 100:P2})" : "";
                var y = (float)GetYCoordinateFromPrice(point, yMin, yMax, CandleTopBottomMargin, LiveActualHeight);
                canvas.DrawLine(0, y, (float)CandleChart.ActualWidth, y, LongHorizontalLinePointerPaint);
                canvas.DrawText($"{point} {changeText}", 2, y - 4, CandleInfoFont, LongHorizontalLinePointerPaint);
            }

            foreach (var point in ShortPoints)
            {
                var changeText = LongPoints.Count > 0 ? $"({Calculator.Roe(PositionSide.Long, LongPoints[0], point) / 100:P2})" : "";
                var y = (float)GetYCoordinateFromPrice(point, yMin, yMax, CandleTopBottomMargin, LiveActualHeight);
                canvas.DrawLine(0, y, (float)CandleChart.ActualWidth, y, ShortHorizontalLinePointerPaint);
                canvas.DrawText($"{point} {changeText}", 2, y - 4, CandleInfoFont, ShortHorizontalLinePointerPaint);
            }
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var cursorPosition = GetCursorPosition();
                var x = (float)cursorPosition.X - (float)CandleChart.PointToScreen(new Point(0, 0)).X;
                var y = (float)cursorPosition.Y - (float)CandleChart.PointToScreen(new Point(0, 0)).Y;
                CurrentMouseY = y;

                if (x < 0 || x >= CandleChart.ActualWidth - CandleChart.ActualWidth / ChartCount)
                {
                    if (CurrentMouseX != -1358)
                    {
                        CurrentMouseX = -1358;
                        CandleChart.InvalidateVisual();
                    }
                    return;
                }

                CurrentMouseX = x;
                CandleChart.InvalidateVisual();
            }
            catch
            {
            }
        }

        Random r = new();
        List<TradeHistory> tradeHistories = new();
        int buyIndex = -1;
        int sellIndex = -1;
        string resultString = string.Empty;
        private void LoadHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                if (dialog.ShowDialog() ?? false)
                {
                    var fileName = dialog.FileName;
                    var data = File.ReadAllLines(fileName);
                    foreach (var line in data)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        var s = line.Split(',');
                        tradeHistories.Add(new TradeHistory(
                            s[1],
                            DateTime.Parse(s[0]),
                            DateTime.Parse(s[3]),
                            (PositionSide)Enum.Parse(typeof(PositionSide), s[2]),
                            (PositionResult)Enum.Parse(typeof(PositionResult), s[4]),
                            decimal.Parse(s[5])
                            ));
                    }
                    MessageBox.Show("Load Complete");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void NextHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var history = tradeHistories[r.Next(tradeHistories.Count)];

            SymbolTextBox.Text = history.Symbol;
            DateTextBox.Text = history.EntryTime.ToString("yyyy-MM-dd");
            CandleCountTextBox.Text = ((history.ExitTime - history.EntryTime).TotalMinutes / 5 + 300).ToString();

            if (history.Side == PositionSide.Long)
            {
                buyIndex = (int)(history.EntryTime - history.EntryTime.ToString("yyyy-MM-dd").ToDateTime()).TotalMinutes / 5;
                sellIndex = (int)(history.ExitTime - history.EntryTime.ToString("yyyy-MM-dd").ToDateTime()).TotalMinutes / 5;
            }
            else if (history.Side == PositionSide.Short)
            {
                buyIndex = (int)(history.ExitTime - history.EntryTime.ToString("yyyy-MM-dd").ToDateTime()).TotalMinutes / 5;
                sellIndex = (int)(history.EntryTime - history.EntryTime.ToString("yyyy-MM-dd").ToDateTime()).TotalMinutes / 5;
            }

            resultString = $"{history.Result}, {history.Income}({(history.Income / 5).Round(2)}%)";

            LoadChart();
        }

        private void RefreshOptionButton_Click(object sender, RoutedEventArgs e)
        {
            LoadChart();
        }

        /// <summary>
        /// 가격에 해당하는 Y좌표
        /// </summary>
        /// <param name="price"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <param name="CandleTopBottomMargin"></param>
        /// <param name="LiveActualHeight"></param>
        /// <returns></returns>
        public decimal GetYCoordinateFromPrice(decimal price, decimal yMin, decimal yMax, decimal CandleTopBottomMargin, float LiveActualHeight)
        {
            return CandleTopBottomMargin + ((yMax - price) / (yMax - yMin) * (decimal)LiveActualHeight);
        }

        private void CandleChart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var pointingChart = CurrentMouseX == -1358 ? Charts[ChartCount - 1] : Charts[(int)(CurrentMouseX / LiveActualItemFullWidth)];

                LongPoints.Clear();
                LongPoints.Add(pointingChart.Quote.Close);
            }
            catch
            {
            }
        }

        private void CandleChart_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var pointingChart = CurrentMouseX == -1358 ? Charts[ChartCount - 1] : Charts[(int)(CurrentMouseX / LiveActualItemFullWidth)];

                ShortPoints.Clear();
                ShortPoints.Add(pointingChart.Quote.Close);
            }
            catch
            {
            }
        }
    }
}
