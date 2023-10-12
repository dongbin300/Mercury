using Mercury;
using Mercury.Charts;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Alphasquare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gaten", "StockData", "Quotes");

        private readonly SKPaint LongPaint = new() { Color = new(59, 207, 134) };
        private readonly SKPaint ShortPaint = new() { Color = new(237, 49, 97) };
        private readonly int CandleTopBottomMargin = 10;
        List<ChartInfo> Charts = new();
        private int ChartCount => Charts.Count;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Chart
        record StockChartInfo(string date, int o, int h, int l, int c, int v);
        /// <summary>
        /// Load chart by code and start date
        /// </summary>
        /// <param name="code">012345</param>
        /// <param name="startDate">20231012</param>
        void LoadChart(string code, string startDate)
        {
            var candleCount = 120;

            Charts.Clear();

            try
            {
                var data = File.ReadAllLines(MercuryPath.Stock1D.Down($"{code}.csv"));
                var charts = new List<StockChartInfo>();
                foreach (var d in data)
                {
                    var e = d.Split(',');
                    charts.Add(new StockChartInfo(e[0], e[1].ToInt(), e[2].ToInt(), e[3].ToInt(), e[4].ToInt(), e[5].ToInt()));
                }

                var startIndex = charts.IndexOf(charts.First(c => c.date.Equals(startDate))) + 15;
                for (int i = startIndex; i >= startIndex - 120; i--)
                {
                    var chart = charts[i];
                    Charts.Add(new ChartInfo(code, new Quote(
                        DateTime.Parse($"{chart.date[0..4]}-{chart.date[4..6]}-{chart.date[6..8]}"),
                        chart.o,
                        chart.h,
                        chart.l,
                        chart.c,
                        chart.v
                        )));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

            // Draw Quote
            for (int i = 0; i < Charts.Count; i++)
            {
                var quote = Charts[i].Quote;
                var viewIndex = i;

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
        }
        #endregion

        record Result(int index, string code, string date);
        List<Result> Results = new();
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            var keyword = OpenTextBox.Text + "," + HighTextBox.Text + "," + LowTextBox.Text + "," + CloseTextBox.Text;

            var fileNames = Directory.GetFiles(basePath, "*.csv");
            ResultListBox.Items.Clear();
            Results.Clear();
            int p = 0;
            foreach (var file in fileNames)
            {
                var data = File.ReadAllLines(file);
                var filter = data.Where(d => d.Contains(keyword));
                if (filter.Any())
                {
                    var code = file[(file.LastIndexOf('\\') + 1)..].Replace(".csv", "");
                    ResultListBox.Items.Add(code);
                    p++;

                    foreach (var f in filter)
                    {
                        var date = f[..8];
                        ResultListBox.Items.Add(date);
                        Results.Add(new Result(p, code, date));
                        p++;
                    }
                }
            }
        }

        private void OpenPerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var ratio = 1 + double.Parse(OpenPerTextBox.Text) / 100;
                var result = (int)(int.Parse(LastCloseTextBox.Text) * ratio);
                OpenTextBox.Text = result.ToString();

                if (TwoDecimalDigitRegex().IsMatch(OpenPerTextBox.Text))
                {
                    HighPerTextBox.Focus();
                }
            }
            catch
            {
            }
        }

        private void HighPerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var ratio = 1 + double.Parse(HighPerTextBox.Text) / 100;
                var result = (int)(int.Parse(LastCloseTextBox.Text) * ratio);
                HighTextBox.Text = result.ToString();

                if (TwoDecimalDigitRegex().IsMatch(HighPerTextBox.Text))
                {
                    LowPerTextBox.Focus();
                }
            }
            catch
            {
            }
        }

        private void LowPerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var ratio = 1 + double.Parse(LowPerTextBox.Text) / 100;
                var result = (int)(int.Parse(LastCloseTextBox.Text) * ratio);
                LowTextBox.Text = result.ToString();

                if (TwoDecimalDigitRegex().IsMatch(LowPerTextBox.Text))
                {
                    ClosePerTextBox.Focus();
                }
            }
            catch
            {
            }
        }

        private void ClosePerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (ClosePerTextBox.Text.EndsWith("c")) // reset
                {
                    DefaultButton_Click(sender, e);
                    return;
                }

                var ratio = 1 + double.Parse(ClosePerTextBox.Text) / 100;
                var result = (int)(int.Parse(LastCloseTextBox.Text) * ratio);
                CloseTextBox.Text = result.ToString();
            }
            catch
            {
            }
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            LastCloseTextBox.Text = OpenPerTextBox.Text = OpenTextBox.Text = HighPerTextBox.Text = HighTextBox.Text = LowPerTextBox.Text = LowTextBox.Text = ClosePerTextBox.Text = CloseTextBox.Text = "";
            LastCloseTextBox.Focus();
        }

        void SetUp(TextBox textBox)
        {
            textBox.Text = (int.Parse(textBox.Text) + 1).ToString();
        }

        void SetDown(TextBox textBox)
        {
            textBox.Text = (int.Parse(textBox.Text) - 1).ToString();
        }

        void SetAuto(TextBox textBox)
        {
            var currentPrice = textBox.Text.ToInt();
            switch (textBox.Text.Length)
            {
                case 3:
                    break;

                case 4:
                    currentPrice = (int)Math.Round(currentPrice / 5.0) * 5;
                    break;

                case 5:
                    currentPrice = (int)Math.Round(currentPrice / 50.0) * 50;
                    break;

                case 6:
                    currentPrice = (int)Math.Round(currentPrice / 500.0) * 500;
                    break;
            }
            textBox.Text = currentPrice.ToString();
        }

        private void OpenUpButton_Click(object sender, RoutedEventArgs e)
        {
            SetUp(OpenTextBox);
        }

        private void OpenDownButton_Click(object sender, RoutedEventArgs e)
        {
            SetDown(OpenTextBox);
        }

        private void HighUpButton_Click(object sender, RoutedEventArgs e)
        {
            SetUp(HighTextBox);
        }

        private void HighDownButton_Click(object sender, RoutedEventArgs e)
        {
            SetDown(HighTextBox);
        }

        private void LowUpButton_Click(object sender, RoutedEventArgs e)
        {
            SetUp(LowTextBox);
        }

        private void LowDownButton_Click(object sender, RoutedEventArgs e)
        {
            SetDown(LowTextBox);
        }

        private void CloseUpButton_Click(object sender, RoutedEventArgs e)
        {
            SetUp(CloseTextBox);
        }

        private void CloseDownButton_Click(object sender, RoutedEventArgs e)
        {
            SetDown(CloseTextBox);
        }

        [GeneratedRegex("^-*[0-9]+[.]+[0-9]{2}$")]
        private static partial Regex TwoDecimalDigitRegex();

        private void ResultListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var result = Results.Find(r => r.index.Equals(ResultListBox.SelectedIndex));
            if (result == null)
            {
                return;
            }

            LoadChart(result.code, result.date);
        }

        private void ClosePerTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FindButton_Click(sender, e);
            }
        }

        private void AutoButton_Click(object sender, RoutedEventArgs e)
        {
            SetAuto(OpenTextBox);
            SetAuto(HighTextBox);
            SetAuto(LowTextBox);
            SetAuto(CloseTextBox);
        }
    }
}
