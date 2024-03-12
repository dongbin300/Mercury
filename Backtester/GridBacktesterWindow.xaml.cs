using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests;
using Mercury.Charts;
using Mercury.Enums;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Backtester
{
	/// <summary>
	/// GridBacktesterWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class GridBacktesterWindow : Window
	{
		BackgroundWorker worker = new()
		{
			WorkerReportsProgress = true
		};

		string symbol = string.Empty;
		KlineInterval interval;
		DateTime startDate;
		DateTime endDate;
		//BacktestType backtestType;
		//string strategyId = string.Empty;
		string reportFileName = string.Empty;

		public GridBacktesterWindow()
		{
			InitializeComponent();

			SymbolTextBox.Text = Settings.Default.Symbol;
			StartDateTextBox.Text = Settings.Default.StartDate;
			EndDateTextBox.Text = Settings.Default.EndDate;
			FileNameTextBox.Text = Settings.Default.FileName;
			IntervalComboBox.SelectedIndex = Settings.Default.IntervalIndex;
			//StrategyComboBox.SelectedIndex = Settings.Default.StrategyIndex;
		}

		private void BacktestButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Settings.Default.Symbol = SymbolTextBox.Text;
				Settings.Default.StartDate = StartDateTextBox.Text;
				Settings.Default.EndDate = EndDateTextBox.Text;
				Settings.Default.FileName = FileNameTextBox.Text;
				Settings.Default.IntervalIndex = IntervalComboBox.SelectedIndex;
				//Settings.Default.StrategyIndex = StrategyComboBox.SelectedIndex;
				Settings.Default.Save();

				symbol = SymbolTextBox.Text;
				interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
				//strategyId = (StrategyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "macd2";
				startDate = StartDateTextBox.Text.ToDateTime();
				endDate = EndDateTextBox.Text.ToDateTime();
				//backtestType = BacktestSymbolRadioButton.IsChecked ?? false ? BacktestType.BySymbol : BacktestType.All;
				reportFileName = FileNameTextBox.Text;

				BacktestProgress.Value = 0;
				BacktestProgress.Maximum = 100;

				worker.DoWork += Worker_DoWork;
				worker.ProgressChanged += (sender, e) =>
				{
					BacktestProgress.Value = e.ProgressPercentage;
				};
				Common.ReportProgress = worker.ReportProgress;
				worker.RunWorkerAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void Worker_DoWork(object? sender, DoWorkEventArgs e)
		{
			try
			{
				ChartLoader.Charts = [];

				Common.ReportProgress(5);
				ChartLoader.InitCharts(symbol, KlineInterval.OneWeek);
				ChartLoader.InitCharts(symbol, KlineInterval.FiveMinutes);

				Common.ReportProgress(15);
				var oneWeekChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.OneWeek);
				oneWeekChartPack.UseAtr();
				var fiveMinuteChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.FiveMinutes);
				fiveMinuteChartPack.UseAtr();

				Common.ReportProgress(30);
				ChartLoader.InitCharts(symbol, interval, startDate, endDate);

				Common.ReportProgress(50);
				var chartPack = ChartLoader.GetChartPack(symbol, interval);
				var firstChart = chartPack.Charts[0];
				var lastWeekAtr = (decimal)oneWeekChartPack.Charts.Where(d => d.DateTime <= firstChart.DateTime).OrderByDescending(d => d.DateTime).ElementAt(1).Atr.Round(1);
				var lastMinuteAtr = (decimal)fiveMinuteChartPack.Charts.Where(d => d.DateTime <= firstChart.DateTime).OrderByDescending(d => d.DateTime).ElementAt(1).Atr.Round(1);

				var upperPrice = firstChart.Quote.Open + lastWeekAtr;
				var lowerPrice = firstChart.Quote.Open - lastWeekAtr;
				var upperStopLossPrice = firstChart.Quote.Close + lastWeekAtr * 1.1M;
				var lowerStopLossPrice = firstChart.Quote.Close - lastWeekAtr * 1.1M;
				var gridInterval = lastMinuteAtr;

				var backtester = new GridBacktester([.. chartPack.Charts], interval, upperPrice, lowerPrice, gridInterval, 1, GridType.Neutral, upperStopLossPrice, lowerStopLossPrice);
				backtester.Run(Common.ReportProgress, reportFileName, 0, 0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void FileOpenButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo()
			{
				FileName = MercuryPath.Desktop.Down($"{FileNameTextBox.Text}.csv"),
				UseShellExecute = true
			});
		}
	}
}
