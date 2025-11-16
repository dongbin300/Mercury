using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using SkiaSharp;
using Mercury;
using Mercury.Charts;
using Binance.Net.Enums;
using System.Threading.Tasks;

namespace ChartViewer2
{
    public partial class MainWindow : Window
    {
        private List<ChartInfo> _chartData = new();
        private DateTime _currentDate = DateTime.Now;
        private KlineInterval _currentInterval = KlineInterval.FiveMinutes;
        private int _candleCount = 500;
        private string _symbol = "BTCUSDT";

        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            SymbolTextBox.Text = _symbol;
            DateTextBox.Text = _currentDate.ToString("yyyy-MM-dd");
            CandleCountTextBox.Text = _candleCount.ToString();

            // Set default checkboxes
            Ma1CheckBox.IsChecked = true;
            Ma2CheckBox.IsChecked = true;
            Ema1CheckBox.IsChecked = true;
        }

        private void InitializeChart()
        {
            CandleChart.XAxes = new Axis[]
            {
                new Axis
                {
                    LabelsRotation = 0,
                    LabelsPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                    TicksPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                    NamePaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                    Name = "Time"
                }
            };

            CandleChart.YAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                    TicksPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(60, 60, 60)),
                    NamePaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                    Name = "Price"
                }
            };

            CandleChart.Background = new SolidColorBrush(Colors.Transparent);
        }

        private async void LoadHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadChartData();
        }

        private async Task LoadChartData()
        {
            try
            {
                // TODO: Implement actual data loading using Mercury services
                // For now, create sample data
                _chartData = GenerateSampleData(_candleCount);

                await Dispatcher.InvokeAsync(UpdateChart);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chart data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ChartInfo> GenerateSampleData(int count)
        {
            var data = new List<ChartInfo>();
            var random = new Random();
            var basePrice = 50000.0;

            for (int i = 0; i < count; i++)
            {
                var variation = (random.NextDouble() - 0.5) * 1000;
                var close = basePrice + variation;
                var high = close + random.NextDouble() * 200;
                var low = close - random.NextDouble() * 200;
                var open = low + random.NextDouble() * (high - low);
                var volume = random.NextDouble() * 1000000;

                // Create sample OHLC data
                var quote = new Quote
                {
                    Date = _currentDate.AddMinutes(i * 5),
                    Open = (decimal)open,
                    High = (decimal)high,
                    Low = (decimal)low,
                    Close = (decimal)close,
                    Volume = (decimal)volume
                };
                var chartInfo = new ChartInfo(_symbol, quote);
                // Store additional data for visualization
                chartInfo.Sma1 = (decimal?)open;
                chartInfo.Sma2 = (decimal?)high;
                chartInfo.Ema1 = (decimal?)low;
                chartInfo.Ema2 = (decimal?)close;

                data.Add(chartInfo);
            }

            return data;
        }

        private void UpdateChart()
        {
            if (_chartData == null || _chartData.Count == 0) return;

            var series = new List<ISeries>();

            // Create candlestick visualization with step lines
            var closeValues = _chartData.Select((c, i) => new ObservablePoint(i, (double)c.Quote.Close)).ToArray();
            var highValues = _chartData.Select((c, i) => new ObservablePoint(i, (double)c.Quote.High)).ToArray();
            var lowValues = _chartData.Select((c, i) => new ObservablePoint(i, (double)c.Quote.Low)).ToArray();
            var openValues = _chartData.Select((c, i) => new ObservablePoint(i, (double)c.Quote.Open)).ToArray();

            // Main line for close prices
            series.Add(new StepLineSeries<ObservablePoint>
            {
                Values = closeValues,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(new SKColor(14, 203, 129)), // Long color
                Fill = null,
                Name = "Close (Candlestick Style)"
            });

            // High-Low range visualization
            var highLowPoints = new List<ObservablePoint>();
            for (int i = 0; i < _chartData.Count; i++)
            {
                highLowPoints.Add(new ObservablePoint(i, (double)_chartData[i].Quote.High));
                highLowPoints.Add(new ObservablePoint(i, (double)_chartData[i].Quote.Low));

                // Add NaN to break the line
                if (i < _chartData.Count - 1)
                {
                    highLowPoints.Add(new ObservablePoint(i, double.NaN));
                }
            }

            series.Add(new LineSeries<ObservablePoint>
            {
                Values = highLowPoints,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(new SKColor(200, 200, 200, 128)), // Semi-transparent gray
                Fill = null,
                Name = "High-Low Range"
            });

            // Add moving averages if checked
            if (Ma1CheckBox.IsChecked == true)
            {
                if (int.TryParse(Ma1Text.Text, out int ma1Period))
                {
                    series.Add(new LineSeries<double>
                    {
                        Values = CalculateMAFromClose(ma1Period),
                        GeometrySize = 0,
                        Stroke = new SolidColorPaint(SKColors.Red),
                        Name = $"MA{ma1Period}"
                    });
                }
            }

            if (Ma2CheckBox.IsChecked == true)
            {
                if (int.TryParse(Ma2Text.Text, out int ma2Period))
                {
                    series.Add(new LineSeries<double>
                    {
                        Values = CalculateMAFromClose(ma2Period),
                        GeometrySize = 0,
                        Stroke = new SolidColorPaint(SKColors.Blue),
                        Name = $"MA{ma2Period}"
                    });
                }
            }

            CandleChart.Series = series;
        }

        private double[] CalculateMAFromClose(int period)
        {
            var prices = _chartData.Select(c => (double)c.Quote.Close).ToArray();
            var ma = new double[prices.Length];

            for (int i = 0; i < prices.Length; i++)
            {
                if (i < period - 1)
                {
                    ma[i] = double.NaN;
                }
                else
                {
                    var sum = 0.0;
                    for (int j = 0; j < period; j++)
                    {
                        sum += prices[i - j];
                    }
                    ma[i] = sum / period;
                }
            }

            return ma;
        }

        private void RefreshOptionButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateChart();
        }

        private void NextHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            DateTextBox.Text = _currentDate.ToString("yyyy-MM-dd");
            _ = LoadChartData();
        }

        private void SymbolTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _symbol = SymbolTextBox.Text?.ToUpper() ?? "BTCUSDT";
        }

        private void DateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DateTime.TryParse(DateTextBox.Text, out var date))
            {
                _currentDate = date;
            }
        }

        private void CandleCountTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(CandleCountTextBox.Text, out int count))
                {
                    _candleCount = Math.Max(100, Math.Min(5000, count));
                    CandleCountTextBox.Text = _candleCount.ToString();
                    _ = LoadChartData();
                }
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // TODO: Implement crosshair functionality
        }

        // Indicator checkbox handlers
        private void Ma1CheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ma1CheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ma2CheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ma2CheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ma3CheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ma3CheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ema1CheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ema1CheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ema2CheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ema2CheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ema3CheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void Ema3CheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void RsiCheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void RsiCheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void CciCheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void CciCheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
        private void AtrCheckBox_Checked(object sender, RoutedEventArgs e) => UpdateChart();
        private void AtrCheckBox_Unchecked(object sender, RoutedEventArgs e) => UpdateChart();
    }
}