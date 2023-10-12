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
            var stoch = quotes.GetStoch(12).Select(x => x.Stoch);
            var _macd = quotes.GetMacd(12, 26, 9);
            var macd = _macd.Select(x => x.Macd);
            var signal = _macd.Select(x => x.Signal);
            var st = quotes.GetSupertrend(10, 1.5).Select(x => x.Supertrend);
            var bbu = quotes.GetBollingerBands(24, 3, QuoteType.High).Select(x => x.Upper);
            var bbl = quotes.GetBollingerBands(24, 3, QuoteType.Low).Select(x => x.Lower);
            for (int i = 0; i < Charts.Count; i++)
            {
                var chart = Charts[i];
                //chart.Adx = adx.ElementAt(i);
                chart.Stoch = stoch.ElementAt(i);
                chart.Macd = macd.ElementAt(i);
                chart.MacdSignal = signal.ElementAt(i);
                chart.Supertrend1 = st.ElementAt(i);
                chart.Bb1Upper = bbu.ElementAt(i);
                chart.Bb1Lower = bbl.ElementAt(i);
            }

            CandleChart.InvalidateVisual();
        }

        private void CandleChart_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (ChartCount <= 0)
            {
                return;
            }

            var actualWidth = (float)CandleChart.ActualWidth;
            var actualHeight = (float)CandleChart.ActualHeight - CandleTopBottomMargin * 2;
            var actualItemFullWidth = actualWidth / ChartCount;
            var actualItemMargin = actualItemFullWidth * 0.2f;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var yMax = (double)Charts.Max(x => x.Quote.High);
            var yMin = (double)Charts.Min(x => x.Quote.Low);

            var macdMax = (double)Charts.Max(x => x.Macd);
            var macdMin = (double)Charts.Min(x => x.Macd);

            // Draw Quote and Indicator
            for (int i = 0; i < Charts.Count; i++)
            {
                var quote = Charts[i].Quote;
                var stoch = Charts[i].Stoch;
                //var adx = Charts[i].Adx;
                var macd = Charts[i].Macd;
                var signal = Charts[i].MacdSignal;
                var st = Charts[i].Supertrend1;
                var bbu = Charts[i].Bb1Upper;
                var bbl = Charts[i].Bb1Lower;
                var preStoch = i == 0 ? stoch : Charts[i - 1].Stoch;
                //var preAdx = i == 0 ? adx : Charts[i - 1].Adx;
                var preMacd = i == 0 ? macd : Charts[i - 1].Macd;
                var preSignal = i == 0 ? signal : Charts[i - 1].MacdSignal;
                var preSt = i == 0 ? st : Charts[i - 1].Supertrend1;
                var preBbu = i == 0 ? bbu : Charts[i - 1].Bb1Upper;
                var preBbl = i == 0 ? bbl : Charts[i - 1].Bb1Lower;
                var viewIndex = i;

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - (preMacd - macdMin) / (macdMax - macdMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - (macd - macdMin) / (macdMax - macdMin)) + CandleTopBottomMargin),
                    new SKPaint() { Color = SKColors.SkyBlue }
                    );

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - (preSignal - macdMin) / (macdMax - macdMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - (signal - macdMin) / (macdMax - macdMin)) + CandleTopBottomMargin),
                    new SKPaint() { Color = SKColors.Orange }
                    );

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - (Math.Abs(preSt) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - (Math.Abs(st) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    st > 0 ? LongPaint : ShortPaint
                    );

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - Math.Abs(preStoch) / 100) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - Math.Abs(stoch) / 100) + CandleTopBottomMargin),
                    new SKPaint() { Color = stoch < 20 || stoch > 80 ? SKColors.Yellow : SKColors.Gray }
                    );

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - (Math.Abs(preBbu) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - (Math.Abs(bbu) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPaint() { Color = SKColors.White }
                    );

                canvas.DrawLine(
                    new SKPoint(
                        actualItemFullWidth * (viewIndex - 0.5f),
                        actualHeight * (float)(1.0 - (Math.Abs(preBbl) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPoint(
                        actualItemFullWidth * (viewIndex + 0.5f),
                        actualHeight * (float)(1.0 - (Math.Abs(bbl) - yMin) / (yMax - yMin)) + CandleTopBottomMargin),
                    new SKPaint() { Color = SKColors.White }
                    );

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

            // Draw Pointer
            canvas.DrawRect(
                (int)(CurrentMouseX / actualItemFullWidth) * actualItemFullWidth,
                0,
                actualItemFullWidth,
                (float)CandleChart.ActualHeight,
                CandlePointerPaint
                );

            // Draw Buy/Sell Pointer
            if (buyIndex != -1 && sellIndex != -1)
            {
                canvas.DrawRect(
               buyIndex * actualItemFullWidth,
               0,
               actualItemFullWidth,
               (float)CandleChart.ActualHeight,
               CandleBuyPointerPaint
               );

                canvas.DrawRect(
               sellIndex * actualItemFullWidth,
               0,
               actualItemFullWidth,
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
            var pointingPrice = ((decimal)(((CandleTopBottomMargin - CurrentMouseY) / actualHeight + 1) * (yMax - yMin) + yMin)).Round(4);
            canvas.DrawText($"{pointingPrice}", 2, CurrentMouseY - 4, CandleInfoFont, CandleInfoPaint);

            // Draw Info Text
            try
            {
                var pointingChart = CurrentMouseX == -1358 ? Charts[ChartCount - 1] : Charts[(int)(CurrentMouseX / actualItemFullWidth)];
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
    }
}
