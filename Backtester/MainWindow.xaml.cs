using Backtester.Views;

using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests;
using Mercury.Charts;
using Mercury.Cryptos;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Backtester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<SimpleDealManager> dealResult = new();
        List<PrecisionBacktestDealManager> pbDealResult = new();

        public MainWindow()
        {
            InitializeComponent();

            SymbolTextBox.Text = Settings.Default.Symbol;
            StartDateTextBox.Text = Settings.Default.StartDate;
            EndDateTextBox.Text = Settings.Default.EndDate;
            FileNameTextBox.Text = Settings.Default.FileName;
            SymbolTextBoxPB.Text = Settings.Default.SymbolPB;
            StartDateTextBoxPB.Text = Settings.Default.StartDatePB;
            EndDateTextBoxPB.Text = Settings.Default.EndDatePB;
            FileNameTextBoxPB.Text = Settings.Default.FileNamePB;

            BySymbolGrid.Visibility = Visibility.Visible;
            BySymbolRectangle.Visibility = Visibility.Visible;
            PrecisionBacktestGrid.Visibility = Visibility.Hidden;
            PrecisionBacktestRectangle.Visibility = Visibility.Hidden;

            IntervalComboBoxPB.SelectedIndex = 2;
            StrategyComboBoxPB.Items.Clear();
            StrategyComboBoxPB.Items.Add("TS1 All");
            StrategyComboBoxPB.Items.Add("TS1 Single");
            StrategyComboBoxPB.Items.Add("LSMA All");
            StrategyComboBoxPB.Items.Add("LSMA Single");
            StrategyComboBoxPB.Items.Add("TS2 All");
            StrategyComboBoxPB.Items.Add("TS2 Single");
            StrategyComboBoxPB.Items.Add("MACD V3 All");
            StrategyComboBoxPB.Items.Add("MACD V3 Single");
            StrategyComboBoxPB.Items.Add("BB All");
            StrategyComboBoxPB.Items.Add("BB Single");
            StrategyComboBoxPB.Items.Add("SMACD All");
            StrategyComboBoxPB.Items.Add("SMACD Single");
            StrategyComboBoxPB.Items.Add("MACD V2 All");
            StrategyComboBoxPB.Items.Add("MACD V2 Single");
            StrategyComboBoxPB.SelectedIndex = 6;

#pragma warning disable CS8625 // Null 리터럴을 null을 허용하지 않는 참조 형식으로 변환할 수 없습니다.
            PrecisionBacktestText_MouseLeftButtonDown(null, null);
#pragma warning restore CS8625 // Null 리터럴을 null을 허용하지 않는 참조 형식으로 변환할 수 없습니다.
        }

        private void SymbolTextBoxPB_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (SymbolTextBoxPB.Text == string.Empty)
                {
                    return;
                }

                SymbolCountTextPB.Text = SymbolTextBoxPB.Text.Split(';').Length.ToString();
            }
            catch
            {
            }
        }

        private void BacktestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Default.Symbol = SymbolTextBox.Text;
                Settings.Default.StartDate = StartDateTextBox.Text;
                Settings.Default.EndDate = EndDateTextBox.Text;
                Settings.Default.FileName = FileNameTextBox.Text;
                Settings.Default.Save();

                var p1 = Parameter1TextBox.Text.ToDecimal();
                var p2 = Parameter2TextBox.Text.ToDecimal();
                var p3 = Parameter3TextBox.Text.ToDecimal();

                dealResult.Clear();
                var symbols = SymbolTextBox.Text.Split(';');
                var interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();

                BacktestProgress.Value = 0;
                BacktestProgress.Maximum = symbols.Length;
                foreach (var symbol in symbols)
                {
                    try
                    {
                        BacktestProgress.Value++;
                        var startDate = StartDateTextBox.Text.ToDateTime() > CryptoSymbol.GetStartDate(symbol).AddDays(1) ? StartDateTextBox.Text.ToDateTime() : CryptoSymbol.GetStartDate(symbol).AddDays(1);
                        var endDate = EndDateTextBox.Text.ToDateTime();

                        if (startDate >= endDate)
                        {
                            continue;
                        }

                        // 차트 로드 및 초기화
                        if (ChartLoader.GetChartPack(symbol, interval) == null)
                        {
                            ChartLoader.InitChartsByDate(symbol, interval, startDate, endDate);
                        }

                        // 차트 진행하면서 매매
                        var charts = ChartLoader.GetChartPack(symbol, interval);

                        StrategyEv(charts, p1);
                    }
                    catch (FileNotFoundException)
                    {
                        continue;
                    }
                }

                foreach (var d in dealResult)
                {
                    var content = $"{d.ChartInfo.Symbol},{d.TargetRoe},{d.TotalIncome.Round(2)},{d.BacktestDays},{d.IncomePerDay.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBox.Text}.csv"), content);

                    if (dealResult.Count == 1)
                    {
                        var resultView = new BacktestResultView();
                        resultView.Init(symbols[0], d);
                        resultView.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StrategyEv(ChartPack charts, decimal p1)
        {
            charts.CalculateIndicatorsEveryonesCoin();

            var dealManager = new SimpleDealManager(charts.Charts[0].DateTime, charts.Charts[^1].DateTime, 100, 1.85m);
            for (int i = 1; i < charts.Charts.Count; i++)
            {
                if (p1 == 0)
                {
                    dealManager.EvaluateEveryonesCoinShort(charts.Charts[i], charts.Charts[i - 1]);
                }
                else if (p1 == 1)
                {
                    dealManager.EvaluateEveryonesCoinLong(charts.Charts[i], charts.Charts[i - 1]);
                }
            }

            // Set latest chart for UPNL
            dealManager.ChartInfo = charts.Charts[^1];
            dealResult.Add(dealManager);
        }

        private void StrategyStefano(ChartPack charts)
        {
            charts.CalculateIndicatorsStefano();

            var dealManager = new SimpleDealManager(charts.Charts[0].DateTime, charts.Charts[^1].DateTime, 100, null, 2.0m);
            for (int i = 1; i < charts.Charts.Count; i++)
            {
                dealManager.EvaluateStefanoLong(charts.Charts[i], charts.Charts[i - 1], 0.2m, 0.5m);
            }
            dealManager.ChartInfo = charts.Charts[^1];
            dealResult.Add(dealManager);
        }

        private void BySymbolText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BySymbolGrid.Visibility = Visibility.Visible;
            BySymbolRectangle.Visibility = Visibility.Visible;
            PrecisionBacktestGrid.Visibility = Visibility.Hidden;
            PrecisionBacktestRectangle.Visibility = Visibility.Hidden;
        }

        private void PrecisionBacktestText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BySymbolGrid.Visibility = Visibility.Hidden;
            BySymbolRectangle.Visibility = Visibility.Hidden;
            PrecisionBacktestGrid.Visibility = Visibility.Visible;
            PrecisionBacktestRectangle.Visibility = Visibility.Visible;
        }

        private void BacktestButtonPB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Default.SymbolPB = SymbolTextBoxPB.Text;
                Settings.Default.StartDatePB = StartDateTextBoxPB.Text;
                Settings.Default.EndDatePB = EndDateTextBoxPB.Text;
                Settings.Default.FileNamePB = FileNameTextBoxPB.Text;
                Settings.Default.Save();

                pbDealResult.Clear();
                var symbols = SymbolTextBoxPB.Text.Split(';');
                var interval = ((IntervalComboBoxPB.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
                var startDate = StartDateTextBoxPB.Text.ToDateTime();
                var endDate = EndDateTextBoxPB.Text.ToDateTime();
                var takeProfitRoe = Parameter1TextBoxPB.Text.ToDecimal();
                var p2 = Parameter2TextBoxPB.Text.ToDouble();

                switch (StrategyComboBoxPB.SelectedIndex)
                {
                    case 0:
                        Strategy1(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 1:
                        Strategy2(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 2:
                        Strategy3(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 3:
                        Strategy4(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 4:
                        Strategy5(symbols, interval, startDate, endDate);
                        break;

                    case 5:
                        Strategy6(symbols, interval, startDate, endDate);
                        break;

                    case 6:
                        Strategy7(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 7:
                        Strategy8(symbols, interval, startDate, endDate);
                        break;

                    case 8:
                        Strategy9(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 10:
                        Strategy11(symbols, interval, startDate, endDate, takeProfitRoe);
                        break;

                    case 11:
                        Strategy12(symbols, interval, startDate, endDate);
                        break;

                    case 12:
                        Strategy13(symbols, interval, startDate, endDate);
                        break;

                    case 13:
                        Strategy14(symbols, interval, startDate, endDate);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 심볼 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void Strategy1(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        ChartLoader.InitChartsAndTs1IndicatorsByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, takeProfitRoe, takeProfitRoe, 100)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            var headerContent = $"{dealManager.StartDate:yyyy-MM-dd HH:mm:ss}~{dealManager.EndDate:yyyy-MM-dd HH:mm:ss}, {interval.ToIntervalString()}" + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), headerContent);
            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 5; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 220)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.EvaluateTsLongNextCandle();
                dealManager.EvaluateTsShortNextCandle();

                if (i % 96 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
                    $"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result}" + Environment.NewLine
                    );
            }

            var resultChartView = new BacktestResultChartView();
            resultChartView.Init(dealManager.PositionHistories, interval);
            resultChartView.Show();
        }

        /// <summary>
        /// 심볼 개별 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void Strategy2(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    ChartLoader.Charts.Clear();
                    ChartLoader.InitChartsAndTs1IndicatorsByDate(symbol, interval, startDate, endDate);
                }
                catch
                {
                }

                var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, takeProfitRoe, takeProfitRoe, 100)
                {
                    MonitoringSymbols = new List<string>() { symbol }
                };
                var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

                ChartLoader.SelectCharts();
                int i = 1;
                for (; i < 5; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);
                }
                for (; i < evaluateCount; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);

                    if (dealManager.Charts[symbol].Count >= 220)
                    {
                        dealManager.RemoveOldChart();
                    }

                    dealManager.EvaluateTsLongNextCandle();
                    dealManager.EvaluateTsShortNextCandle();
                }

                var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
            }
        }

        /// <summary>
        /// LSMA 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy3(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 33; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 50)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.CalculateIndicatorsLsma();
                dealManager.EvaluateLsmaLongNextCandle();
                dealManager.EvaluateLsmaShortNextCandle();

                if (i % 96 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.SimplePnl.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.SimplePnl.Round(2)}" + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
                    $"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result}" + Environment.NewLine
                    );
            }
        }

        /// <summary>
        /// LSMA 개별 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy4(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {

        }

        /// <summary>
        /// TS2 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void Strategy5(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        ChartLoader.InitChartsAndTs2IndicatorsByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 100)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            var headerContent = $"TS2, {dealManager.StartDate:yyyy-MM-dd}~{dealManager.EndDate:yyyy-MM-dd}, {interval.ToIntervalString()}" + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), headerContent);
            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 20; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 200)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.EvaluateTs2LongBit();
                dealManager.EvaluateTs2ShortBit();

                if (i % 720 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            var hi = 1;
            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"PH-TS2.csv"),
                    $"{hi++},{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{h.Income}" + Environment.NewLine
                    );
            }

            var resultChartView = new BacktestResultChartView();
            resultChartView.Init(dealManager.PositionHistories, interval);
            resultChartView.Show();
        }

        /// <summary>
        /// TS2 개별 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy6(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    ChartLoader.Charts.Clear();
                    ChartLoader.InitChartsAndTs2IndicatorsByDate(symbol, interval, startDate, endDate);
                }
                catch
                {
                }

                var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 100)
                {
                    MonitoringSymbols = new List<string>() { symbol }
                };
                var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

                ChartLoader.SelectCharts();
                int i = 1;
                for (; i < 20; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);
                }
                for (; i < evaluateCount; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);

                    if (dealManager.Charts[symbol].Count >= 200)
                    {
                        dealManager.RemoveOldChart();
                    }

                    dealManager.EvaluateTs2LongBit();
                    dealManager.EvaluateTs2ShortBit();
                }

                var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
            }
        }

        /// <summary>
        /// MACD V3 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy7(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        //ChartLoader.InitChartsByDate(symbol, interval, startDate, endDate);
                        ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 5, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 240; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 260)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.CalculateIndicatorsMacd();
                dealManager.EvaluateMacdV3LongNextCandle();
                dealManager.EvaluateMacdV3ShortNextCandle();

                if (i % 288 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
                    $"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
                    );
            }
            File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);

            var resultChartView = new BacktestResultChartView();
            resultChartView.Init(dealManager.PositionHistories, interval);
            resultChartView.Show();
        }

        /// <summary>
        /// MACD V3 개별 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void Strategy8(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    ChartLoader.Charts.Clear();
                    ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                }
                catch
                {
                }

                var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 3, 1.0m, 1.0m, 0.2m)
                {
                    MonitoringSymbols = new List<string>() { symbol }
                };
                var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

                ChartLoader.SelectCharts();
                int i = 1;
                for (; i < 240; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);
                }
                for (; i < evaluateCount; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);

                    if (dealManager.Charts[symbol].Count >= 260)
                    {
                        dealManager.RemoveOldChart();
                    }

                    dealManager.CalculateIndicatorsMacd();
                    dealManager.EvaluateMacdV3LongNextCandle();
                    dealManager.EvaluateMacdV3ShortNextCandle();
                }

                var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
            }
        }

        /// <summary>
        /// BB 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy9(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 240; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 260)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.CalculateIndicatorsBb();
                dealManager.EvaluateBbLongNextCandle();
                dealManager.EvaluateBbShortNextCandle();

                if (i % 288 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
                    $"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
                    );
            }

            var resultChartView = new BacktestResultChartView();
            resultChartView.Init(dealManager.PositionHistories, interval);
            resultChartView.Show();
        }

        /// <summary>
        /// SMACD 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy11(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 240; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            dealManager.CalculateInitSmacd();
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 260)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.CalculateIndicatorsSmacd();
                dealManager.EvaluateSmacdLongNextCandle();
                dealManager.EvaluateSmacdShortNextCandle();

                if (i % 288 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.PositionHistories.Count},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.PositionHistories.Count},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
                    $"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.EntryCount},{Math.Round(h.Income, 4)}" + Environment.NewLine
                    );
            }

            var resultChartView = new BacktestResultChartView();
            resultChartView.Init(dealManager.PositionHistories, interval);
            resultChartView.Show();
        }

        /// <summary>
        /// SMACD 개별 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void Strategy12(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    ChartLoader.Charts.Clear();
                    ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                }
                catch
                {
                }

                var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 0.2m)
                {
                    MonitoringSymbols = new List<string>() { symbol }
                };
                var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

                ChartLoader.SelectCharts();
                int i = 1;
                for (; i < 240; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);
                }
                dealManager.CalculateInitSmacd();
                for (; i < evaluateCount; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);

                    if (dealManager.Charts[symbol].Count >= 260)
                    {
                        dealManager.RemoveOldChart();
                    }

                    dealManager.CalculateIndicatorsSmacd();
                    dealManager.EvaluateSmacdLongNextCandle();
                    dealManager.EvaluateSmacdShortNextCandle();
                }

                var _content = $"{symbol},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
            }
        }

        /// <summary>
        /// MACD V2 전체 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="takeProfitRoe"></param>
        private void Strategy13(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    // 차트 로드 및 초기화
                    if (ChartLoader.GetChartPack(symbol, interval) == null)
                    {
                        ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                    }
                }
                catch
                {
                }
            }

            var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, 2, 1, 0.2m)
            {
                MonitoringSymbols = symbols.ToList()
            };
            var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

            ChartLoader.SelectCharts();
            int i = 1;
            for (; i < 240; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);
            }
            for (; i < evaluateCount; i++)
            {
                var nextCharts = ChartLoader.NextCharts();
                dealManager.ConcatenateChart(nextCharts);

                if (dealManager.Charts[symbols[0]].Count >= 260)
                {
                    dealManager.RemoveOldChart();
                }

                dealManager.CalculateIndicatorsMacd();
                dealManager.EvaluateMacdV2LongNextCandle();
                dealManager.EvaluateMacdV2ShortNextCandle();

                if (i % 288 == 0)
                {
                    var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                    File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
                }
            }

            var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

            foreach (var h in dealManager.PositionHistories)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
                    $"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
                    );
            }
            File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);

            var resultChartView = new BacktestResultChartView();
            resultChartView.Init(dealManager.PositionHistories, interval);
            resultChartView.Show();
        }

        /// <summary>
        /// MACD V2 개별 테스트
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="interval"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void Strategy14(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
        {
            foreach (var symbol in symbols)
            {
                try
                {
                    ChartLoader.Charts.Clear();
                    ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
                }
                catch
                {
                }

                var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 0.2m)
                {
                    MonitoringSymbols = new List<string>() { symbol }
                };
                var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

                ChartLoader.SelectCharts();
                int i = 1;
                for (; i < 240; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);
                }
                for (; i < evaluateCount; i++)
                {
                    var nextCharts = ChartLoader.NextCharts();
                    dealManager.ConcatenateChart(nextCharts);

                    if (dealManager.Charts[symbol].Count >= 260)
                    {
                        dealManager.RemoveOldChart();
                    }

                    dealManager.CalculateIndicatorsMacd();
                    dealManager.EvaluateMacdV2LongNextCandle();
                    dealManager.EvaluateMacdV2ShortNextCandle();
                }

                var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
                File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
            }
        }
    }
}
