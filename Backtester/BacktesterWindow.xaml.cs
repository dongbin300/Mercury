using Binance.Net.Enums;

using CryptoExchange.Net.CommonObjects;

using Mercury;
using Mercury.Apis;
using Mercury.Backtests;
using Mercury.Backtests.BacktestStrategies;
using Mercury.Charts;
using Mercury.Cryptos;
using Mercury.Data;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
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
		KlineInterval subInterval;
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
			SubIntervalComboBox.SelectedIndex = Settings.Default.SubIntervalIndex;
			MaxActiveDealsTypeComboBox.SelectedIndex = Settings.Default.MaxActiveDealsTypeIndex;
			MaxActiveDealsTextBox.Text = Settings.Default.MaxActiveDeals;
			MoneyTextBox.Text = Settings.Default.Money;
			LeverageTextBox.Text = Settings.Default.Leverage;

			// 강제 매크로3 클릭
			WindowState = WindowState.Minimized;

			//SmartRandom r = new SmartRandom();
			//SymbolTextBox.Text = r.Next(LocalApi.GetSymbolNames());
			//var startDate = r.Next(new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));
			//var endDate = startDate.AddDays(365);
			//StartDateTextBox.Text = startDate.ToString("yyyy-MM-dd");
			//EndDateTextBox.Text = endDate.ToString("yyyy-MM-dd");

			BacktestButton_Click(BacktestMacroRButton, new RoutedEventArgs());
		}

		public void For(double start, double end, double step, Action<double> action)
		{
			for (double i = start; i <= end; i += step)
				action(i);
		}

		public void For(int start, int end, int step, Action<int> action)
		{
			for (int i = start; i <= end; i += step)
				action(i);
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
				Settings.Default.SubIntervalIndex = SubIntervalComboBox.SelectedIndex;
				Settings.Default.MaxActiveDealsTypeIndex = MaxActiveDealsTypeComboBox.SelectedIndex;
				Settings.Default.MaxActiveDeals = MaxActiveDealsTextBox.Text;
				Settings.Default.Money = MoneyTextBox.Text;
				Settings.Default.Leverage = LeverageTextBox.Text;
				Settings.Default.Save();

				symbols = SymbolTextBox.Text.Split(';');
				interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
				subInterval = ((SubIntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "1m").ToKlineInterval();
				strategyId = (StrategyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "macd2";
				startDate = StartDateTextBox.Text.ToDateTime();
				endDate = EndDateTextBox.Text.ToDateTime();
				backtestType = BacktestSymbolRadioButton.IsChecked ?? false ? BacktestType.BySymbol : BacktestType.All;
				reportFileName = FileNameTextBox.Text;
				maxActiveDealsType = Enum.Parse<MaxActiveDealsType>((MaxActiveDealsTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Each");
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

					case "BACKTEST MACRO 2":
						worker.DoWork += Worker_DoWorkMacro2;
						break;

					case "BACKTEST MACRO 3":
						worker.DoWork += Worker_DoWorkMacro3;
						break;

					case "RANDOM MACRO":
						worker.DoWork += Worker_DoWorkMacroR;
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
						ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
					}
					List<ChartPack> chartPacks = [];
					for (int i = 0; i < symbols.Length; i++)
					{
						chartPacks.Add(ChartLoader.GetChartPack(symbols[i], interval));
					}

					maxActiveDealsType = MaxActiveDealsType.Total;
					maxActiveDeals = 24;
					leverage = 5;
					var backtester = new Candle7(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
					{
						IsGeneratePositionHistory = false,
						FeeRate = 0.0002m
					};
					backtester.Init(chartPacks);
					backtester.Run(startDate.AddDays(8));
				}
				else if (backtestType == BacktestType.BySymbol)
				{
					for (int i = 0; i < symbols.Length; i++)
					{
						//Common.ReportProgress((int)((double)i / symbols.Length * 50));
						ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
					}

					if (interval != subInterval)
					{
						for (int i = 0; i < symbols.Length; i++)
						{
							ChartLoader.InitCharts(symbols[i], subInterval, startDate, endDate);
						}
					}

					for (int i = 0; i < symbols.Length; i++)
					{
						//Common.ReportProgress(50 + (int)((double)i / symbols.Length * 50));
						int[] leverages = [leverage, leverage, leverage, leverage, leverage, leverage, leverage];
						var backtester = new EasyBacktester(strategyId, [symbols[i]], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
						{
							IsGeneratePositionHistory = false
						};
						backtester.InitIndicators();
						backtester.Run(backtestType, Common.ReportProgress, reportFileName, 200);
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

				if (interval != subInterval)
				{
					for (int i = 0; i < symbols.Length; i++)
					{
						ChartLoader.InitCharts(symbols[i], subInterval, startDate, endDate);
					}
				}

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
					$"{symbols[0]} +{symbols.Length - 1},{interval},{strategyId},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

				for (var maxActiveDeals = 3; maxActiveDeals <= 10; maxActiveDeals++)
				{
					for (var leverage = 3; leverage <= 10; leverage++)
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

									int[] leverages = [leverage, leverage, leverage, leverage, leverage, leverage, leverage];
									var backtester = new EasyBacktester(strategyId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
									{
										IsGeneratePositionHistory = false
									};
									backtester.InitIndicators();
									backtester.Run(backtestType, Common.ReportProgress, reportFileName, 200);

									File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
										$"{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)}" + Environment.NewLine);


									//maxActiveDealsType = MaxActiveDealsType.Each;
									//var backtester1 = new EasyBacktester(strategyId, [.. symbols], interval, subInterval, maxActiveDealsType, maxActiveDeals, money, leverage)
									//{
									//	IsGeneratePositionHistory = false
									//};
									//backtester1.InitIndicators();
									//backtester1.Run(backtestType, Common.ReportProgress, reportFileName, 200);

									//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									//	$"{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)}" + Environment.NewLine);
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

		/// <summary>
		/// Symbol Evaluation + Macro Evaluation
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Worker_DoWorkMacro2(object? sender, DoWorkEventArgs e)
		{
			try
			{
				ChartLoader.Charts = [];

				for (int i = 0; i < symbols.Length; i++)
				{
					ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
				}

				//if (interval != subInterval)
				//{
				//	for (int i = 0; i < symbols.Length; i++)
				//	{
				//		ChartLoader.InitCharts(symbols[i], subInterval, startDate, endDate);
				//	}
				//}

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
					$"{symbols[0]} +{symbols.Length - 1},{interval},{strategyId},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

				var entryStrategy = "custom-1-7";
				var exitStrategy = "custom-2-1";

				//for (var lossPer = -8.0m; lossPer >= -8.0m; lossPer -= 1.0m)
				{
					//for (var banHour = 24; banHour <= 24; banHour += 6)
					{
						//for (var bodyLengthMin = 0.05m; bodyLengthMin <= 0.05m; bodyLengthMin += 0.05m)
						{
							/* Symbol Eval */
							//var symbolResults = new Dictionary<string, decimal>();
							//for (int i = 0; i < symbols.Length; i++)
							//{
							//	int[] leverages = [leverage, leverage, leverage, leverage, leverage, leverage, leverage];
							//	//var backtester = new EasyBacktester(strategyId, [symbols[i]], interval, maxActiveDealsType, maxActiveDeals, money, leverages, lossPer, banHour, bodyLengthMin)
							//	var backtester = new EasyBacktester(strategyId, [symbols[i]], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
							//	{
							//		IsGeneratePositionHistory = false,
							//		StrategyId = entryStrategy,
							//		ExitStrategyId = exitStrategy,
							//		FeeRate = 0.0004m
							//	};
							//	backtester.InitIndicators();
							//	(var symbol, var est) = backtester.Run(BacktestType.BySymbol, Common.ReportProgress, reportFileName, 200);
							//	symbolResults.Add(symbol, est);
							//}
							//var bestSymbols = symbolResults.OrderByDescending(x => x.Value).Take(25).Select(x => x.Key).ToArray();
							////File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
							////	$"({lossPer.Round(1)}/{banHour}/{bodyLengthMin.Round(3)}) Best 25 Symbols: {string.Join(';', bestSymbols)}" + Environment.NewLine);
							//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
							//	$"Best 25 Symbols: {string.Join(';', bestSymbols)}" + Environment.NewLine);

							/* Macro Eval */
							for (var maxActiveDeals = 5; maxActiveDeals <= 30; maxActiveDeals++)
							{
								for (var leverage = 5; leverage <= 5; leverage++)
								{
									int[] leverages = [leverage, leverage, leverage, leverage, leverage, leverage, leverage];

									maxActiveDealsType = MaxActiveDealsType.Total;
									var backtester = new EasyBacktester(strategyId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
									{
										IsGeneratePositionHistory = false,
										StrategyId = entryStrategy,
										ExitStrategyId = exitStrategy,
										FeeRate = 0.0004m
									};
									backtester.InitIndicators();
									backtester.Run(backtestType, Common.ReportProgress, reportFileName, 200);

									//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									//	$"{maxActiveDealsType},{lossPer.Round(1)},{banHour},{bodyLengthMin.Round(3)},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.mMPer.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);
									File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
										$"{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.mMPer.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);


									maxActiveDealsType = MaxActiveDealsType.Each;
									var backtester1 = new EasyBacktester(strategyId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
									{
										IsGeneratePositionHistory = false,
										StrategyId = entryStrategy,
										ExitStrategyId = exitStrategy,
										FeeRate = 0.0004m
									};
									backtester1.InitIndicators();
									backtester1.Run(backtestType, Common.ReportProgress, reportFileName, 200);

									File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
										$"{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)},{backtester1.mMPer.Round(4):P},{backtester1.ResultPerRisk.Round(4)}" + Environment.NewLine);
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

		private void Worker_DoWorkMacro3(object? sender, DoWorkEventArgs e)
		{
			try
			{
				List<ChartPack> chartPacks = [];
				List<ChartPack> chartPacks2 = [];

				ChartLoader.Charts = [];
				for (int i = 0; i < symbols.Length; i++)
				{
					var chartPack = ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
					if (chartPack.Charts.Count <= 0)
					{
						continue;
					}
					chartPacks.Add(chartPack);
				}
				//ChartLoader.InitCharts("BTCUSDT", KlineInterval.OneDay, startDate, endDate);
				//chartPacks2.Add(ChartLoader.GetChartPack("BTCUSDT", KlineInterval.OneDay));

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
					$"{symbols[0]} +{symbols.Length - 1},{interval},{strategyId},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

				#region Comment

				//for (var emaPeriod = 60; emaPeriod <= 60; emaPeriod += 5)
				//{
				//	for (var s0c = 25; s0c <= 25; s0c += 5)
				//	{
				//		for (var s2c = 12; s2c <= 12; s2c += 2)
				//		{
				//			for (var sl = 25; sl <= 25; sl += 5)
				//			{
				//				for (var tp = 65; tp <= 65; tp += 5)
				//				{
				//					//maxActiveDealsType = MaxActiveDealsType.Total;
				//					//var backtester = new TS1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//					//{
				//					//	IsGeneratePositionHistory = false,
				//					//	FeeRate = 0.0004m
				//					//};
				//					//backtester.Init(chartPacks, 20, 2, 60, 3, 120, 6);
				//					//backtester.Run(startDate.AddDays(8));

				//					//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//					//$"TS1,{interval.ToIntervalString()},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.mMPer.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

				//					maxActiveDealsType = MaxActiveDealsType.Each;
				//					var backtester1 = new Ema1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//					{
				//						IsGeneratePositionHistory = false,
				//						FeeRate = 0.0004m,
				//						EmaPeriod = emaPeriod,
				//						Stage0Count = s0c,
				//						Stage2Count = s2c,
				//						SlCount = sl,
				//						TpCount = tp
				//					};
				//					backtester1.Init(chartPacks);
				//					backtester1.Run(startDate.AddDays(8));

				//					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//					$"EMA1,{interval.ToIntervalString()},{emaPeriod},{s0c},{s2c},{sl},{tp},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)},{backtester1.mMPer.Round(4):P},{backtester1.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//				}
				//			}
				//		}
				//	}
				//}

				//for (var pb = 20; pb <= 20; pb += 5)
				//{
				//	for (var mw = 25; mw <= 25; mw += 5)
				//	{
				//		for (var md = 500; md <= 500; md += 2)
				//		{
				//			for (var nn = 100; nn <= 100; nn += 5)
				//			{
				//				for (var ps = 20; ps <= 20; ps += 5)
				//				{
				//					maxActiveDealsType = MaxActiveDealsType.Each;
				//					var backtester1 = new MLMIP1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//					{
				//						IsGeneratePositionHistory = false,
				//						FeeRate = 0.0004m,
				//						ProfitRatio = 1.0m,
				//					};
				//					backtester1.Init(chartPacks, pb, mw, md, nn, ps);
				//					backtester1.Run(startDate.AddDays(8));

				//					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//					$"MLMIP1,{interval.ToIntervalString()},{pb},{mw},{md},{nn},{ps},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)},{backtester1.mMPer.Round(4):P},{backtester1.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//				}
				//			}
				//		}
				//	}
				//}

				//for (var ap = 5; ap <= 30; ap += 5)
				//{
				//	for (var am = 1.0m; am <= 5.0m; am += 0.5m)
				//	{
				//		for (var rp = 5; rp <= 30; rp += 5)
				//		{
				//			for (var mfp = 12; mfp <= 12; mfp += 12)
				//			{
				//				for (var msp = 26; msp <= 26; msp += 26)
				//				{
				//					for (var msip = 9; msip <= 9; msip += 9)
				//					{
				//						maxActiveDealsType = MaxActiveDealsType.Each;
				//						var backtester1 = new TrendRiderTest(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//						{
				//							IsGeneratePositionHistory = false,
				//							FeeRate = 0.0004m,
				//						};
				//						backtester1.Init(chartPacks, ap, am, rp, mfp, msp, msip);
				//						backtester1.Run(startDate.AddDays(8));

				//						File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//						$"TrendRiderTest,{interval.ToIntervalString()},{ap},{am},{rp},{mfp},{msp},{msip},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)},{backtester1.mMPer.Round(4):P},{backtester1.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//					}
				//				}
				//			}
				//		}
				//	}
				//}

				//maxActiveDealsType = MaxActiveDealsType.Total;
				//var backtester = new SM1(reportFileName, money, 5, maxActiveDealsType, 1)
				//{
				//	IsGeneratePositionHistory = false,
				//	FeeRate = 0.0004m,
				//};
				//backtester.Init(chartPacks, 20, 2.0m, 20, 1.5m);
				//backtester.InitIndicator2(chartPacks2);
				//backtester.Run(startDate.AddDays(8));

				//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//	$"SM1,{interval.ToIntervalString()},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.mMPer.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

				// RsiH 값이 73~77일때 높아지는 경향이 있음
				// BTCUSDT;ETHUSDT;BNBUSDT;XRPUSDT;ADAUSDT;SOLUSDT;DOGEUSDT;DOTUSDT;AVAXUSDT;LTCUSDT
				//BTCUSDT;XRPUSDT;LTCUSDT;TRXUSDT;ETCUSDT;XLMUSDT;XMRUSDT;BNBUSDT;VETUSDT;NEOUSDT;IOSTUSDT;THETAUSDT;DOGEUSDT;BANDUSDT;MKRUSDT;YFIUSDT;RUNEUSDT;SUSHIUSDT;SOLUSDT;UNIUSDT;AVAXUSDT;FTMUSDT;KSMUSDT;AAVEUSDT;LRCUSDT;AXSUSDT;ALPHAUSDT;ZENUSDT;GRTUSDT;CHZUSDT;COTIUSDT;HBARUSDT;MASKUSDT;ARUSDT;LPTUSDT;ENSUSDT

				//for (double l = 35; l <= 45; l += 3)
				//for (double h = 42; h <= 64; h += 3)
				// 35, 65, 55, 50

				//maxActiveDealsType = MaxActiveDealsType.Total;
				//var leverage = 5;
				//var maxActiveDeals = 10;
				//var backtester = new CGTrend1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//{
				//	IsGeneratePositionHistory = false,
				//	FeeRate = 0.0002m,
				//	RsiL = 35,
				//	RsiH = 60,
				//	RsiL2 = 50,
				//	RsiH2 = 50
				//};

				//backtester.Init(chartPacks);
				//backtester.InitIndicator2(chartPacks2);
				//backtester.Run(startDate.AddDays(8));

				//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//	$"CGTrend1,{interval.ToIntervalString()},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);
				#endregion

				// BTCUSDT;BNBUSDT;SUSHIUSDT;ZECUSDT;BAKEUSDT

				//For(20, 200, 20, a1 =>	// SmaPeriod
				//{
				//	For(38, 38, 5, a2 =>   // RsiLongThreshold
				//	{
				//		For(0.8, 0.8, 0.2, a3 =>    // StopLossAtrMultiplier
				//		{
				//			For(0.5, 0.5, 0.5, a4 =>    // NewStopLossAtrMultiplier
				//			{
				//				For(10, 10, 5, a5 => // MaxHoldBars
				//				{
				//					maxActiveDealsType = MaxActiveDealsType.Total;
				//					var leverage = 1;
				//					var maxActiveDeals = 5;
				//					var backtester = new MarketAdaptive2(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//					{
				//						IsGeneratePositionHistory = false,
				//						FeeRate = 0.00035m,
				//						//SmaPeriod = a1,
				//						RsiLongThreshold = a2,
				//						StopLossAtrMultiplier = a3,
				//						NewStopLossAtrMultiplier = a4,
				//						MaxHoldBars = a5
				//					};

				//					backtester.Init(chartPacks);
				//					backtester.Run(startDate.AddDays(30));

				//					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//						$"MarketAdaptive,{interval.ToIntervalString()},{a2},{a3},{a4},{a5},{maxActiveDealsType}," +
				//						$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//				});
				//			});
				//		});
				//	});
				//}

				maxActiveDealsType = MaxActiveDealsType.Total;
				var leverage = 1;
				var maxActiveDeals = 1;
				var backtester = new ST1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				{
					IsGeneratePositionHistory = false,
					FeeRate = 0.0002m
				};

				backtester.Init(chartPacks);
				backtester.Run(startDate.AddDays(30));

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
					$"MarketAdaptive,{interval.ToIntervalString()},{maxActiveDealsType}," +
					$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

				//var maxActiveDeals = 35;
				//int[] leverages = [leverage, leverage, leverage, leverage, leverage, leverage, leverage];
				//int[] entrys = [7, 9, 13, 15, 19, 25, 31];// Enumerable.Range(1, 46).ToArray();
				//int[] exits = [1, 2]; Enumerable.Range(1, 22).ToArray();

				//foreach(var entry in entrys)
				//{
				//for (var leverage = 20; leverage <= 80; leverage += 3)
				//foreach(var exit in exits)
				//{
				//maxActiveDealsType = MaxActiveDealsType.Each;
				//var backtester = new Candle102(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//{
				//	IsGeneratePositionHistory = false,
				//	FeeRate = 0.0004m
				//};
				//backtester.Init(chartPacks);
				//backtester.Run(startDate.AddDays(8));

				//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//	$"{interval.ToIntervalString()},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.mMPer.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

				//maxActiveDealsType = MaxActiveDealsType.Total;
				//var backtester1 = new Candle102(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//{
				//	IsGeneratePositionHistory = false,
				//	FeeRate = 0.0004m
				//};
				//backtester1.Init(chartPacks);
				//backtester1.Run(startDate.AddDays(8));

				//var entryId = "custom-1-7";// + entry.ToString("D2");
				//var exitId = "custom-2-1";// + exit.ToString("D2");
				//var symbolResults = new Dictionary<string, decimal>();
				//for (int i = 0; i < symbols.Length; i++)
				//{
				//	var backtester = new EasyBacktester(entryId, [symbols[i]], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
				//	{
				//		IsGeneratePositionHistory = false,
				//		ExitStrategyId = exitId,
				//		FeeRate = 0.0004m
				//	};
				//	backtester.InitIndicators();
				//	(var symbol, var est) = backtester.Run(BacktestType.BySymbol, Common.ReportProgress, reportFileName, 200);
				//	symbolResults.Add(symbol, est);
				//}
				//var bestSymbols = symbolResults.OrderByDescending(x => x.Value).Take(25).Select(x => x.Key).ToArray();
				//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//	$"({entryId}/{exitId}) Best 25 Symbols: {string.Join(';', bestSymbols)}" + Environment.NewLine);

				//	maxActiveDealsType = MaxActiveDealsType.Each;
				//	var backtester1 = new EasyBacktester(entryId, [.. symbols], interval, maxActiveDealsType, maxActiveDeals, money, leverages)
				//	{
				//		IsGeneratePositionHistory = false,
				//		ExitStrategyId = exitId,
				//		FeeRate = 0.0004m
				//	};
				//	backtester1.InitIndicators();
				//	backtester1.Run(backtestType, Common.ReportProgress, reportFileName, 200);

				//	File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//		$"{entryId},{exitId},{interval.ToIntervalString()},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester1.Win},{backtester1.Lose},{backtester1.WinRate.Round(2)},{backtester1.EstimatedMoney.Round(0)},{backtester1.mMPer.Round(4):P},{backtester1.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//}
				//}

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									"END" + Environment.NewLine + Environment.NewLine + Environment.NewLine);

				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private record BacktestR_Result(int Win, int Lose, decimal EstimatedMoney, decimal Mdd, decimal ResultPerRisk);
		/// <summary>
		/// Date, Symbol Random
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Worker_DoWorkMacroR(object? sender, DoWorkEventArgs e)
		{
			try
			{
				var randomResults = new List<BacktestR_Result>();
				var r = new SmartRandom();
				var symbols = LocalApi.GetSymbolNames();
				var period = 365;

				for (int count = 0; count < 20; count++)
				{
					List<ChartPack> chartPacks = [];
					ChartLoader.Charts = [];

					var symbol = r.Next(symbols);
					var symbolListingDate = CryptoSymbol.GetStartDate(symbol).AddDays(1);
					var startDate = r.Next(symbolListingDate, new DateTime(2024, 2, 28));
					var endDate = startDate.AddDays(period);

					var chartPack = ChartLoader.InitCharts(symbol, interval, startDate, endDate);
					if (chartPack.Charts.Count <= 0)
					{
						continue;
					}
					chartPacks.Add(chartPack);

					maxActiveDealsType = MaxActiveDealsType.Total;
					var leverage = 1;
					var maxActiveDeals = 1;
					var backtester = new ST1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
					{
						IsGeneratePositionHistory = false,
						FeeRate = 0.0002m
					};

					backtester.Init(chartPacks);
					backtester.Run(startDate.AddDays(10));

					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
						$"{symbol},{backtester.GetType().Name},{interval.ToIntervalString()},{maxActiveDealsType}," +
						$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

					randomResults.Add(new BacktestR_Result(backtester.Win, backtester.Lose, backtester.EstimatedMoney, backtester.Mdd, backtester.ResultPerRisk));
				}

				var totalWin = randomResults.Sum(x => x.Win);
				var totalLose = randomResults.Sum(x => x.Lose);
				var averageEstimatedMoney = randomResults.Average(x => x.EstimatedMoney);
				var maxMdd = randomResults.Max(x => x.Mdd);
				var averageResultPerRisk = randomResults.Average(x => x.ResultPerRisk);

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									$"{totalWin},{totalLose},{(totalWin + totalLose > 0 ? (decimal)totalWin / (totalWin + totalLose) : 0).Round(4):P}," +
									$"{averageEstimatedMoney.Round(0)},{maxMdd.Round(4):P},{averageResultPerRisk.Round(4)}" + Environment.NewLine);

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									"END" + Environment.NewLine + Environment.NewLine + Environment.NewLine);

				Environment.Exit(0);

				//ChartLoader.Charts = [];
				//for (int i = 0; i < symbols.Length; i++)
				//{
				//	ChartLoader.InitCharts(symbols[i], interval, startDate, endDate);
				//}

				//List<ChartPack> chartPacks = [];
				//for (int i = 0; i < symbols.Length; i++)
				//{
				//	chartPacks.Add(ChartLoader.GetChartPack(symbols[i], interval));
				//}

				//for (int i = 0; i < 200; i++)
				//{
				//	var symbolCount = 35;
				//	var periodDays = 180;
				//	var maxActiveDealsType = MaxActiveDealsType.Total;
				//	var maxActiveDeals = 15;
				//	var leverage = 5;

				//	var rSymbols = symbols.OrderBy(x => random.Next()).Take(symbolCount).ToList();
				//	var minStartDate = startDate.AddDays(10);
				//	var maxStartDate = endDate.AddDays(-periodDays);
				//	var totalDays = (maxStartDate - minStartDate).Days;
				//	var rStartDate = minStartDate.AddDays(random.Next(totalDays + 1));
				//	var rEndDate = rStartDate.AddDays(periodDays);

				//	var backtester = new TS1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//	{
				//		IsGeneratePositionHistory = false,
				//		FeeRate = 0.0004m,
				//		ProfitRatio = 1.13m
				//	};
				//	backtester.Init(chartPacks, 30, 2.5m, 85, 5, 90, 11);
				//	backtester.Run(rStartDate, rEndDate);

				//	File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//	$"{string.Join(';', rSymbols)},{interval.ToIntervalString()},TS1,{rStartDate:yyyy-MM-dd},{rEndDate:yyyy-MM-dd},{maxActiveDealsType},{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.mMPer.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//}
				//File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//					"END" + Environment.NewLine + Environment.NewLine + Environment.NewLine);

				//Environment.Exit(0);
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

		private void IntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SubIntervalComboBox == null)
			{
				return;
			}

			SubIntervalComboBox.SelectedIndex = IntervalComboBox.SelectedIndex;
		}
	}
}
