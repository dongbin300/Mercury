using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests;
using Mercury.Charts;
using Mercury.Data;
using Mercury.Enums;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
		MaxActiveDealsType maxActiveDealsType;
		int maxActiveDeals;
		decimal money;
		int leverage;
		string strategyId = string.Empty;
		string reportFileName = string.Empty;

		Random random = new Random();

		public BacktesterWindow()
		{
			InitializeComponent();

			SymbolTextBox.Text = Settings.Default.Symbol;
			StartDateTextBox.Text = Settings.Default.StartDate;
			EndDateTextBox.Text = Settings.Default.EndDate;
			FileNameTextBox.Text = Settings.Default.FileName;
			StrategyComboBox.SelectedIndex = Settings.Default.StrategyIndex;
			IntervalComboBox.SelectedIndex = Settings.Default.IntervalIndex;
			MaxActiveDealsTypeComboBox.SelectedIndex = Settings.Default.MaxActiveDealsTypeIndex;
			MaxActiveDealsTextBox.Text = Settings.Default.MaxActiveDeals;
			MoneyTextBox.Text = Settings.Default.Money;
			LeverageTextBox.Text = Settings.Default.Leverage;
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
				if (sender is not Button button)
				{
					return;
				}

				Settings.Default.Symbol = SymbolTextBox.Text;
				Settings.Default.StartDate = StartDateTextBox.Text;
				Settings.Default.EndDate = EndDateTextBox.Text;
				Settings.Default.FileName = FileNameTextBox.Text;
				Settings.Default.StrategyIndex = StrategyComboBox.SelectedIndex;
				Settings.Default.IntervalIndex = IntervalComboBox.SelectedIndex;
				Settings.Default.MaxActiveDealsTypeIndex = MaxActiveDealsTypeComboBox.SelectedIndex;
				Settings.Default.MaxActiveDeals = MaxActiveDealsTextBox.Text;
				Settings.Default.Money = MoneyTextBox.Text;
				Settings.Default.Leverage = LeverageTextBox.Text;
				Settings.Default.Save();

				symbols = SymbolTextBox.Text.Split(';');
				interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
				strategyId = (StrategyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "macd2";
				startDate = StartDateTextBox.Text.ToDateTime();
				endDate = EndDateTextBox.Text.ToDateTime();
				backtestType = BacktestSymbolRadioButton.IsChecked ?? false ? BacktestType.BySymbol : BacktestType.All;
				reportFileName = FileNameTextBox.Text;
				maxActiveDealsType = (MaxActiveDealsType)Enum.Parse(typeof(MaxActiveDealsType), (MaxActiveDealsTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Each");
				maxActiveDeals = MaxActiveDealsTextBox.Text.ToInt();
				money = MoneyTextBox.Text.ToDecimal();
				leverage = LeverageTextBox.Text.ToInt();

				BacktestProgress.Value = 0;
				BacktestProgress.Maximum = symbols.Length * 2;

				worker.ProgressChanged += (sender, e) =>
				{
					BacktestProgress.Value = e.ProgressPercentage;
				};
				Common.ReportProgress = worker.ReportProgress;

				switch (button.Content)
				{
					case "BACKTEST":
						worker.DoWork += Worker_DoWork;
						break;

					case "BACKTEST MACRO":
						worker.DoWork += Worker_DoWorkMacro;
						break;
				}

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
				if (backtestType == BacktestType.All)
				{
					for (int i = 0; i < symbols.Length; i++)
					{
						//Common.ReportProgress((int)((double)i / symbols.Length * 50));
						ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
					}

					var backtester = new EasyBacktester(strategyId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverage)
					{
						IsGeneratePositionHistory = true
					};
					backtester.InitIndicators();
					backtester.Run(backtestType, Common.ReportProgress, reportFileName, 24, 240);
				}
				else if (backtestType == BacktestType.BySymbol)
				{
					for (int i = 0; i < symbols.Length; i++)
					{
						//Common.ReportProgress((int)((double)i / symbols.Length * 50));
						ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
					}

					for (int i = 0; i < symbols.Length; i++)
					{
						//Common.ReportProgress(50 + (int)((double)i / symbols.Length * 50));
						var backtester = new EasyBacktester(strategyId, [symbols[i]], interval, maxActiveDealsType, maxActiveDeals, money, leverage)
						{
							IsGeneratePositionHistory = false
						};
						backtester.InitIndicators();
						backtester.Run(backtestType, Common.ReportProgress, reportFileName, 1, 240);
					}
				}

				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Worker_DoWorkMacro(object? sender, DoWorkEventArgs e)
		{
			try
			{
				ChartLoader.Charts = [];

				for (int i = 0; i < symbols.Length; i++)
				{
					ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
				}

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
					$"{symbols[0]} +{symbols.Length - 1},{interval},{strategyId},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

				for (var maxActiveDeals = 3; maxActiveDeals <= 20; maxActiveDeals++)
				{
					for (var leverage = 1; leverage <= 10; leverage++)
					{
						//for (var macd2 = 10; macd2 <= 10; macd2++)
						{
							//for (var st = 5; st <= 5; st += 5)
							{
								//for (var stf = 3.0m; stf <= 3.0m; stf += 0.5m)
								{
									maxActiveDealsType = MaxActiveDealsType.Total;
									////var macd1Values = MacdTable.GetValues(macd1);
									////var macd2Values = MacdTable.GetValues(macd2);

									var backtester = new EasyBacktester(strategyId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverage)
									{
										IsGeneratePositionHistory = false
									};
									backtester.InitIndicators();
									backtester.Run(backtestType, Common.ReportProgress, reportFileName, 24 * 12, 240);

									File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
										$"{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)}" + Environment.NewLine);


									maxActiveDealsType = MaxActiveDealsType.Each;
									var backtester1 = new EasyBacktester(strategyId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverage)
									{
										IsGeneratePositionHistory = false
									};
									backtester1.InitIndicators();
									backtester1.Run(backtestType, Common.ReportProgress, reportFileName, 24 * 12, 240);

									File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
										$"{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)}" + Environment.NewLine);
								}
							}
						}
					}
				}


				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									"END" + Environment.NewLine + Environment.NewLine + Environment.NewLine);

				Environment.Exit(0);
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

		private void RandomDateButton_Click(object sender, RoutedEventArgs e)
		{
			var startDate = new DateTime(2022, 7, 18);
			var endDate = new DateTime(2024, 5, 31);
			var range = (endDate - startDate).Days;
			var randomStartDate = startDate.AddDays(random.Next(range));
			var startDate1 = randomStartDate.AddMonths(1);
			var endDate1 = new DateTime(2024, 6, 30);
			var range1 = (endDate1 - startDate1).Days;
			var randomEndDate = startDate1.AddDays(random.Next(range1));

			StartDateTextBox.Text = randomStartDate.ToString("yyyy-MM-dd");
			EndDateTextBox.Text = randomEndDate.ToString("yyyy-MM-dd");
		}
	}
}
