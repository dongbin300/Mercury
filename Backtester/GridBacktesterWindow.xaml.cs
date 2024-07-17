using Binance.Net.Enums;

using MarinerXX.Apis;

using Mercury;
using Mercury.Backtests;
using Mercury.Backtests.Calculators;
using Mercury.Charts;
using Mercury.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
				if (intervalString == "price")
				{
					worker.DoWork -= Worker_DoWorkPrice;
					worker.DoWork += Worker_DoWorkPrice;
				}
				else
				{
					interval = intervalString.ToKlineInterval();
					worker.DoWork -= Worker_DoWork;
					worker.DoWork += Worker_DoWork;
				}
				worker.ProgressChanged += (sender, e) =>
				{
					BacktestProgress.Value = e.ProgressPercentage;
				};
				Common.ReportProgress = worker.ReportProgress;
				Common.ReportProgressCount = (i, m) =>
				{
					DispatcherService.Invoke(() =>
					{
						BacktestProgressText.Text = $"{i} / {m}";
					});
				};
				worker.RunWorkerAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		List<Price> aggregatedTrades = [];
		private void Worker_DoWorkPrice(object? sender, DoWorkEventArgs e)
		{
			try
			{
				DispatcherService.Invoke(() =>
				{
					ChartLoader.Charts = [];
					var interval = KlineInterval.OneDay;
					ChartLoader.InitCharts(symbol, interval);
					var chartPack = ChartLoader.GetChartPack(symbol, interval);

					if (aggregatedTrades.Count <= 0)
					{
						aggregatedTrades = ChartLoader.GetAggregatedTrades(Common.ReportProgressCount, symbol, startDate, endDate);
					}

					var periodList = new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
					//var periodList = new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 450, 500 };
					//var period = Param1TextBox.Text.ToInt();
					//var factor = Param2TextBox.Text.ToDecimal();
					//var gridCount = GridCountTextBox.Text.ToInt();

					var gridCount = 60;
					var factor = 1.5m;
					//for (var gridCount = 10; gridCount <= 200; gridCount += 10)
					{
						//for (var factor = 1.5m; factor <= 4.0m; factor += 0.5m)
						{
							for (var pi = 0; pi < periodList.Length; pi++)
							{
								var period = periodList[pi];
								chartPack.UsePredictiveRanges(period, (double)factor);

								var backtester = new GridPredictiveRangesBacktester(symbol, aggregatedTrades, [.. chartPack.Charts], reportFileName, gridCount);
								var result = backtester.Run(0);
								var resultString = $"{period},{factor.Round(1)},{gridCount}," + result;
								File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
						 resultString + Environment.NewLine);
							}
						}
					}
				});
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

				//var backtester = new GridBacktester([.. chartPack.Charts], [.. oneWeekChartPack.Charts], [.. oneDayChartPack.Charts], [.. fiveMinuteChartPack.Charts], 1.0M, GridType.Neutral);
				//backtester.Run(Common.ReportProgress, reportFileName, 1440, 0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Worker_DoWorkGridRange(object? sender, DoWorkEventArgs e)
		{
			try
			{
				List<decimal> results = [];
				ChartLoader.Charts = [];
				ChartLoader.InitCharts(symbol, interval, new DateTime(2019, 9, 9), endDate);
				var chartPack = ChartLoader.GetChartPack(symbol, interval);

				for (int i = 2; i <= 400; i++)
				{
					chartPack.UsePredictiveRanges(i, 11.0);

					var estimator = new GridRangeEstimator(symbol, [.. chartPack.Charts.Skip(1057)]);
					estimator.Run(0);
					//BacktestResultListBox.Items.Add($"PERIOD {i} / FACTOR {j} / " + estimator.Money);
					results.Add(estimator.Money);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Worker_DoWorkRisk(object? sender, DoWorkEventArgs e)
		{
			try
			{
				List<decimal> results = [];
				ChartLoader.Charts = [];
				ChartLoader.InitCharts(symbol, interval, new DateTime(2019, 9, 9), endDate);
				var chartPack = ChartLoader.GetChartPack(symbol, interval);

				var periodList = new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
				//var periodList = new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 450, 500 };

				var gridCount = 60;
				var factor = 1.5m;
				//for (var gridCount = 10; gridCount <= 200; gridCount += 10)
				{
					//for (var factor = 1.5m; factor <= 4.0m; factor += 0.5m)
					{
						for (var pi = 0; pi < periodList.Length; pi++)
						{
							var period = periodList[pi];
							chartPack.UsePredictiveRanges(period, (double)factor);

							var calculator = new PredictiveRangesRiskCalculator(symbol, [.. chartPack.Charts], gridCount);
							var result = calculator.Run(784);
							var resultString = $"{period},{factor.Round(1)},{gridCount}," + result;
							File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
					 resultString + Environment.NewLine);
						}
					}
				}
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

		private void GridRangeButton_Click(object sender, RoutedEventArgs e)
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
				interval = intervalString.ToKlineInterval();
				worker.DoWork -= Worker_DoWorkGridRange;
				worker.DoWork += Worker_DoWorkGridRange;

				worker.ProgressChanged += (sender, e) =>
				{
					BacktestProgress.Value = e.ProgressPercentage;
				};
				Common.ReportProgress = worker.ReportProgress;
				Common.ReportProgressCount = (i, m) =>
				{
					DispatcherService.Invoke(() =>
					{
						BacktestProgressText.Text = $"{i} / {m}";
					});
				};
				worker.RunWorkerAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CalculateRiskButton_Click(object sender, RoutedEventArgs e)
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
				interval = intervalString.ToKlineInterval();
				worker.DoWork -= Worker_DoWorkRisk;
				worker.DoWork += Worker_DoWorkRisk;

				worker.ProgressChanged += (sender, e) =>
				{
					BacktestProgress.Value = e.ProgressPercentage;
				};
				Common.ReportProgress = worker.ReportProgress;
				Common.ReportProgressCount = (i, m) =>
				{
					DispatcherService.Invoke(() =>
					{
						BacktestProgressText.Text = $"{i} / {m}";
					});
				};
				worker.RunWorkerAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void BactestRiskButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Common.ReportProgressCount = (i, m) =>
				{
					DispatcherService.Invoke(() =>
					{
						BacktestProgressText.Text = $"{i} / {m}";
					});
				};

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

				ChartLoader.Charts = [];
				var interval = KlineInterval.OneDay;
				ChartLoader.InitCharts(symbol, interval);
				var chartPack = ChartLoader.GetChartPack(symbol, interval);
				aggregatedTrades = ChartLoader.GetAggregatedTrades(Common.ReportProgressCount, symbol, startDate, endDate);

				//var gridCount = 60;
				//var factor = 1.5m;
				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"), $"{symbol},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd}" + Environment.NewLine);

				for (var gridCount = 10; gridCount <= 60; gridCount += 2)
				{
					for (var factor = 1.5m; factor <= 1.5m; factor += 0.1m)
					{
						for (var period = 34; period <= 34; period++)
						{
							chartPack.UsePredictiveRanges(period, (double)factor);

							var backtester = new GridPredictiveRangesBacktester(symbol, aggregatedTrades, [.. chartPack.Charts], reportFileName, gridCount);
							var result = backtester.Run(0);
							var estimatedMoney = decimal.Parse(result.Split(',')[2]);

							var riskCalculator = new PredictiveRangesRiskCalculator(symbol, [.. chartPack.Charts], gridCount);
							var startIndex = chartPack.Charts.IndexOf(chartPack.Charts.First(x => x.DateTime.Year == startDate.Year && x.DateTime.Month == startDate.Month && x.DateTime.Day == startDate.Day));
							var riskResult = riskCalculator.Run(startIndex);
							var leveragedEstimatedMoney = (int)(riskResult * (estimatedMoney - 1_000_000));

							var resultString = $"{period},{factor.Round(1)},{gridCount},{result},{riskResult.Round(2)},{leveragedEstimatedMoney}";
							File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"), resultString + Environment.NewLine);
						}
					}
				}

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"), "END" + Environment.NewLine + Environment.NewLine);

				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void BactestRisk2Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Common.ReportProgressCount = (i, m) =>
				{
					DispatcherService.Invoke(() =>
					{
						BacktestProgressText.Text = $"{i} / {m}";
					});
				};

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

				ChartLoader.Charts = [];
				var interval = KlineInterval.OneDay;
				ChartLoader.InitCharts(symbol, interval);
				var chartPack = ChartLoader.GetChartPack(symbol, interval);
				aggregatedTrades = ChartLoader.GetAggregatedTrades(Common.ReportProgressCount, symbol, startDate, endDate);

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"), $"{symbol},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd}" + Environment.NewLine);

				for (var gridCount = 10; gridCount <= 10; gridCount += 5)
				{
					for (var factor = 2.0m; factor <= 2.0m; factor += 0.5m)
					{
						for (var period = 60; period <= 60; period += 10)
						{
							chartPack.UsePredictiveRanges(period, (double)factor);

							var riskCalculator = new PredictiveRangesRiskCalculator2(symbol, [.. chartPack.Charts], gridCount, 0.1m);
							//var startIndex = chartPack.Charts.IndexOf(chartPack.Charts.First(x => x.DateTime.Year == startDate.Year && x.DateTime.Month == startDate.Month && x.DateTime.Day == startDate.Day));
							chartPack.Charts = riskCalculator.Run(300);

							var backtester = new GridPredictiveRangesBacktester2(symbol, aggregatedTrades, [.. chartPack.Charts], reportFileName, gridCount);
							var result = backtester.Run(0);
							var estimatedMoney = decimal.Parse(result.Split(',')[1]);

							var resultString = $"{period},{factor.Round(1)},{gridCount},{result},{estimatedMoney}";
							File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"), resultString + Environment.NewLine);
						}
					}
				}

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"), "END" + Environment.NewLine + Environment.NewLine);

				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
	}
}
