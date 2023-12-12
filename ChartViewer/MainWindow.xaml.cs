using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests;
using Mercury.Charts;

using Microsoft.Win32;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private readonly SKFont CandleInfoFont = new(SKTypeface.FromFamilyName("Meiryo UI"), 11);
        private readonly SKPaint CandleInfoPaint = new() { Color = SKColors.White };
        private readonly SKPaint HorizontalLinePointerPaint = new() { Color = SKColors.Silver };
        private readonly SKPaint LongPaint = new() { Color = new(59, 207, 134) };
        private readonly SKPaint ShortPaint = new() { Color = new(237, 49, 97) };
        private readonly SKPaint CandlePointerPaint = new() { Color = new SKColor(255, 255, 255, 32) };
        private readonly SKPaint CandleBuyPointerPaint = new() { Color = new SKColor(59, 207, 134, 64) };
        private readonly SKPaint CandleSellPointerPaint = new() { Color = new SKColor(237, 49, 97, 64) };
        private readonly int CandleTopBottomMargin = 10;
        List<ChartInfo> Charts = new();
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
                LoadChart();
            }
        }

        void LoadChart()
        {
            var symbol = SymbolTextBox.Text;
            var interval = (IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToKlineInterval() ?? Binance.Net.Enums.KlineInterval.FiveMinutes;
            var candleCount = CandleCountTextBox.Text.ToInt();
            var startDate = DateTextBox.Text.ToDateTime();
            var endDate = startDate.AddSeconds((int)interval * candleCount);
            ChartLoader.Charts.Clear();
            ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
            Charts = ChartLoader.Charts[0].Charts.ToList();

            // Calculate Indicators
            var quotes = Charts.Select(x => x.Quote);
            //var adx = quotes.GetAdx(14, 14).Select(x => x.Adx);
            //var stoch = quotes.GetStoch(12).Select(x => x.Stoch);
            //var _macd = quotes.GetMacd(22, 48, 9);
            //var _macd2 = quotes.GetMacd(11, 24, 9);
            //var macd = _macd.Select(x => x.Macd);
            //var macd2 = _macd2.Select(x => x.Macd);
            //var signal = _macd.Select(x => x.Signal);
            //var st = quotes.GetSupertrend(10, 1.5).Select(x => x.Supertrend);
            //var bbu = quotes.GetBollingerBands(24, 3, QuoteType.High).Select(x => x.Upper);
            //var bbl = quotes.GetBollingerBands(24, 3, QuoteType.Low).Select(x => x.Lower);
            if (Ema1CheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(Ema1Text.Text.ToInt()).Select(x => x.Ema);
                for (int i = 0; i < Charts.Count; i++)
                {
                    Charts[i].Ema1 = ema.ElementAt(i) == 0 ? -39909 : ema.ElementAt(i);
                }
            }
            if (Ema2CheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(Ema2Text.Text.ToInt()).Select(x => x.Ema);
                for (int i = 0; i < Charts.Count; i++)
                {
                    Charts[i].Ema2 = ema.ElementAt(i) == 0 ? -39909 : ema.ElementAt(i);
                }
            }
            if (Ema3CheckBox.IsChecked ?? true)
            {
                var ema = quotes.GetEma(Ema3Text.Text.ToInt()).Select(x => x.Ema);
                for (int i = 0; i < Charts.Count; i++)
                {
                    Charts[i].Ema3 = ema.ElementAt(i) == 0 ? -39909 : ema.ElementAt(i);
                }
            }

            CandleChart.InvalidateVisual();
        }

        private void DrawIndicator(SKCanvas canvas, int viewIndex, double preValue, double value, double max, double min, SKColor color)
        {
            if (preValue == -39909 || value == -39909)
            {
                return;
            }

            canvas.DrawLine(
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex - 0.5f),
                        LiveActualHeight * (float)(1.0 - (preValue - min) / (max - min)) + CandleTopBottomMargin),
                    new SKPoint(
                        LiveActualItemFullWidth * (viewIndex + 0.5f),
                        LiveActualHeight * (float)(1.0 - (value - min) / (max - min)) + CandleTopBottomMargin),
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

            var yMax = (double)Charts.Max(x => x.Quote.High);
            var yMin = (double)Charts.Min(x => x.Quote.Low);

            if (Ema1CheckBox.IsChecked ?? true)
            {
                yMax = Math.Max(yMax, (double)Charts.Max(x => x.Ema1));
                yMin = Math.Min(yMin, (double)Charts.Where(x => x.Ema1 != -39909).Min(x => x.Ema1));
            }
            if (Ema2CheckBox.IsChecked ?? true)
            {
                yMax = Math.Max(yMax, (double)Charts.Max(x => x.Ema2));
                yMin = Math.Min(yMin, (double)Charts.Where(x => x.Ema2 != -39909).Min(x => x.Ema2));
            }
            if (Ema3CheckBox.IsChecked ?? true)
            {
                yMax = Math.Max(yMax, (double)Charts.Max(x => x.Ema3));
                yMin = Math.Min(yMin, (double)Charts.Where(x => x.Ema3 != -39909).Min(x => x.Ema3));
            }

            // Draw Quote and Indicator
            for (int i = 0; i < Charts.Count; i++)
            {
                var quote = Charts[i].Quote;

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

                #region Candle
                canvas.DrawLine(
                    new SKPoint(
                        LiveActualItemFullWidth * (i + 0.5f),
                        LiveActualHeight * (float)(1.0 - ((double)quote.High - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        LiveActualItemFullWidth * (i + 0.5f),
                        LiveActualHeight * (float)(1.0 - ((double)quote.Low - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    quote.Open < quote.Close ? LongPaint : ShortPaint);
                canvas.DrawRect(
                    new SKRect(
                        LiveActualItemFullWidth * i + LiveActualItemMargin / 2,
                        LiveActualHeight * (float)(1.0 - ((double)quote.Open - yMin) / (yMax - yMin)) + CandleTopBottomMargin,
                        LiveActualItemFullWidth * (i + 1) - LiveActualItemMargin / 2,
                        LiveActualHeight * (float)(1.0 - ((double)quote.Close - yMin) / (yMax - yMin)) + CandleTopBottomMargin
                        ),
                    quote.Open < quote.Close ? LongPaint : ShortPaint
                    );
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
            var pointingPrice = ((decimal)(((CandleTopBottomMargin - CurrentMouseY) / LiveActualHeight + 1) * (yMax - yMin) + yMin)).Round(4);
            canvas.DrawText($"{pointingPrice}", 2, CurrentMouseY - 4, CandleInfoFont, CandleInfoPaint);

            // Draw Info Text
            try
            {
                var pointingChart = CurrentMouseX == -1358 ? Charts[ChartCount - 1] : Charts[(int)(CurrentMouseX / LiveActualItemFullWidth)];
                var changeText = pointingChart.Quote.Close >= pointingChart.Quote.Open ? $"+{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}" : $"{(pointingChart.Quote.Close - pointingChart.Quote.Open) / pointingChart.Quote.Open:P2}";
                canvas.DrawText($"{pointingChart.DateTime:yyyy-MM-dd HH:mm:ss}, O {pointingChart.Quote.Open} H {pointingChart.Quote.High} L {pointingChart.Quote.Low} C {pointingChart.Quote.Close}", 3, 10, CandleInfoFont, CandleInfoPaint);
            }
            catch
            {
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
    }
}
