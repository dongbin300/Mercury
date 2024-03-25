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
		string reportFileName = string.Empty;

		public GridBacktesterWindow()
		{
			InitializeComponent();

			SymbolTextBox.Text = Settings.Default.Symbol;
			StartDateTextBox.Text = Settings.Default.StartDate;
			EndDateTextBox.Text = Settings.Default.EndDate;
			FileNameTextBox.Text = Settings.Default.FileName;
			IntervalComboBox.SelectedIndex = Settings.Default.IntervalIndex;
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
				Settings.Default.Save();

				symbol = SymbolTextBox.Text;
				startDate = StartDateTextBox.Text.ToDateTime();
				endDate = EndDateTextBox.Text.ToDateTime();
				reportFileName = FileNameTextBox.Text;

				BacktestProgress.Value = 0;
				BacktestProgress.Maximum = 100;

				var intervalString = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m");
				if(intervalString == "price")
				{
					worker.DoWork += Worker_DoWorkPrice;
				}
				else
				{
					interval = intervalString.ToKlineInterval();
					worker.DoWork += Worker_DoWork;
				}
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
		private void Worker_DoWorkPrice(object? sender, DoWorkEventArgs e)
		{
			try
			{
				ChartLoader.Charts = [];

				Common.ReportProgress(5);
				ChartLoader.InitCharts(symbol, KlineInterval.OneWeek);
				ChartLoader.InitCharts(symbol, KlineInterval.OneDay);
				ChartLoader.InitCharts(symbol, KlineInterval.ThirtyMinutes);

				Common.ReportProgress(15);
				var oneWeekChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.OneWeek);
				oneWeekChartPack.UseAtr();
				var oneDayChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.OneDay);
				oneDayChartPack.UseMacd();
				var fiveMinuteChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.ThirtyMinutes);
				fiveMinuteChartPack.UseAtr();

				Common.ReportProgress(30);
				var aggregatedTrades = ChartLoader.GetAggregatedTrades(symbol, startDate, endDate);

				Common.ReportProgress(50);
				var backtester = new GridDetailBacktester(symbol, aggregatedTrades, [.. oneWeekChartPack.Charts], [.. oneDayChartPack.Charts], [.. fiveMinuteChartPack.Charts], 1.0M, GridType.Neutral);
				backtester.Run(Common.ReportProgress, reportFileName, 0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Worker_DoWork(object? sender, DoWorkEventArgs e)
		{
			try
			{
				ChartLoader.Charts = [];

				Common.ReportProgress(5);
				ChartLoader.InitCharts(symbol, KlineInterval.OneWeek);
				ChartLoader.InitCharts(symbol, KlineInterval.OneDay);
				ChartLoader.InitCharts(symbol, KlineInterval.ThirtyMinutes);

				Common.ReportProgress(15);
				var oneWeekChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.OneWeek);
				oneWeekChartPack.UseAtr();
				var oneDayChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.OneDay);
				oneDayChartPack.UseMacd();
				var fiveMinuteChartPack = ChartLoader.GetChartPack(symbol, KlineInterval.ThirtyMinutes);
				fiveMinuteChartPack.UseAtr();

				Common.ReportProgress(30);
				ChartLoader.InitCharts(symbol, interval, startDate, endDate);

				Common.ReportProgress(50);
				var chartPack = ChartLoader.GetChartPack(symbol, interval);

				var backtester = new GridBacktester([.. chartPack.Charts], [.. oneWeekChartPack.Charts], [.. oneDayChartPack.Charts], [.. fiveMinuteChartPack.Charts], 1.0M, GridType.Neutral);
				backtester.Run(Common.ReportProgress, reportFileName, 1440, 0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
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
