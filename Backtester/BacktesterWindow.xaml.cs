using Backtester.Enums;

using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests;
using Mercury.Charts;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Backtester
{
	/// <summary>
	/// BacktesterWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class BacktesterWindow : Window
	{
		BackgroundWorker worker = new()
		{
			WorkerReportsProgress = true
		};

		string[] symbols = [];
		KlineInterval interval;
		DateTime startDate;
		DateTime endDate;
		BacktestType backtestType;
		string strategyId = string.Empty;
		string reportFileName = string.Empty;

		public BacktesterWindow()
		{
			InitializeComponent();

			SymbolTextBox.Text = Settings.Default.Symbol;
			StartDateTextBox.Text = Settings.Default.StartDate;
			EndDateTextBox.Text = Settings.Default.EndDate;
			FileNameTextBox.Text = Settings.Default.FileName;
		}

		private void SymbolTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				if (SymbolTextBox.Text == string.Empty)
				{
					return;
				}

				SymbolCountText.Text = SymbolTextBox.Text.Split(';').Length.ToString();
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

				symbols = SymbolTextBox.Text.Split(';');
				interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
				strategyId = (StrategyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "macd2";
				startDate = StartDateTextBox.Text.ToDateTime();
				endDate = EndDateTextBox.Text.ToDateTime();
				backtestType = BacktestSymbolRadioButton.IsChecked ?? false ? BacktestType.BySymbol : BacktestType.All;
				reportFileName = FileNameTextBox.Text;

				BacktestProgress.Value = 0;
				BacktestProgress.Maximum = symbols.Length * 2;

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
				if (backtestType == BacktestType.All)
				{
					for (int i = 0; i < symbols.Length; i++)
					{
						Common.ReportProgress((int)((double)i / symbols.Length * 50));
						var symbol = symbols[i];
						ChartLoader.InitCharts(symbol, interval, startDate, endDate);
					}

					var backtester = new EasyBacktester(strategyId, [.. symbols], interval);
					backtester.SetMaxActiveDeals(Mercury.Enums.MaxActiveDealsType.Each, 10);
					backtester.InitIndicators();
					backtester.Run(Common.ReportProgress, reportFileName, 288);
				}
				else if (backtestType == BacktestType.BySymbol)
				{

				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
	}
}
