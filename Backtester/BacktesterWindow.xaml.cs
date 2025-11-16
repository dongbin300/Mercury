using Binance.Net.Enums;

using Mercury;
using Mercury.Apis;
using Mercury.Backtests;
using Mercury.Backtests.BacktestStrategies;
using Mercury.Charts;
using Mercury.Cryptos;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Backtester
{
	/// <summary>
	/// BacktesterWindow.xaml에 대한 상호 작용 논리
	/// Backtester V2 기획
	/// 
	/// - (궁극) 한번의 실행으로 여러가지 경우의 백테스팅을 수행(메모리 부족 문제 해결해야함)
	/// - 다양한 옵션 지원(DCA/Grid 모드, 전략 파라미터 설정, 롱/숏 ON/OFF, 보고서출력 ON/OFF, 랜덤 심볼,일자 등)
	/// - 심볼 조회 기능 개선(현재 사용가능한 심볼 리스트, 상장일~끝일 등 심볼별 정보 조회)
	/// - 백테스트 결과 시각화 개선(기본적으로 csv로 저장, csv파일 불러와서 시각화)
	/// 
	/// 
	/// 
	/// 
	/// 2025-07-31 심볼리스트 저장 (2024-01-01 이전 상장된것들)
	/// BTCUSDT;ETHUSDT;BCHUSDT;XRPUSDT;LTCUSDT;TRXUSDT;ETCUSDT;XLMUSDT;ADAUSDT;XMRUSDT;BNBUSDT;VETUSDT;NEOUSDT;THETAUSDT;DOGEUSDT;BANDUSDT;RLCUSDT;MKRUSDT;DEFIUSDT;YFIUSDT;TRBUSDT;SUSHIUSDT;EGLDUSDT;SOLUSDT;UNIUSDT;AVAXUSDT;ENJUSDT;KSMUSDT;AAVEUSDT;RSRUSDT;LRCUSDT;ZENUSDT;GRTUSDT;SANDUSDT;COTIUSDT;HBARUSDT;MTLUSDT;BTCDOMUSDT;MASKUSDT;ARUSDT;LPTUSDT;ENSUSDT;DUSKUSDT;IMXUSDT;API3USDT;APEUSDT;WOOUSDT;JASMYUSDT;OPUSDT;INJUSDT;LDOUSDT;ICPUSDT;APTUSDT;QNTUSDT;FETUSDT;FXSUSDT;HIGHUSDT;ASTRUSDT;PHBUSDT;GMXUSDT;CFXUSDT;STXUSDT;ACHUSDT;SSVUSDT;CKBUSDT;LQTYUSDT;USDCUSDT;IDUSDT;JOEUSDT;HFTUSDT;XVSUSDT;BLURUSDT;SUIUSDT;NMRUSDT;XVGUSDT;WLDUSDT;PENDLEUSDT;ARKMUSDT;AGLDUSDT;DODOXUSDT;BNTUSDT;OXTUSDT;BIGTIMEUSDT;RIFUSDT;POLYXUSDT;TIAUSDT;CAKEUSDT;TWTUSDT;ORDIUSDT;STEEMUSDT;ILVUSDT;KASUSDT;BEAMXUSDT;PYTHUSDT;SUPERUSDT;USTCUSDT;ONGUSDT;ETHWUSDT;JTOUSDT;AUCTIONUSDT;ACEUSDT;MOVRUSDT;NFPUSDT

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
		KlineInterval forceInterval;

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

			var intervals = new List<string>() { "1m", "5m", "15m", "30m", "1h", "2h", "4h", "1D" };
			IntervalComboBox.ItemsSource = intervals;
			SubIntervalComboBox.ItemsSource = intervals;

			// 강제 매크로3 클릭
			WindowState = WindowState.Minimized;
			SymbolTextBox.Text = "BTCUSDT;ETHUSDT;BCHUSDT;XRPUSDT;LTCUSDT;TRXUSDT;ETCUSDT;XLMUSDT;ADAUSDT;XMRUSDT;BNBUSDT;VETUSDT;NEOUSDT;THETAUSDT;DOGEUSDT;BANDUSDT;RLCUSDT;MKRUSDT;DEFIUSDT";
			StartDateTextBox.Text = "2025-01-01";
			EndDateTextBox.Text = "2025-06-30";
			forceInterval = KlineInterval.TwoHour;
			//SymbolTextBox.Text = "BTCUSDT;XRPUSDT;LTCUSDT;TRXUSDT;ETCUSDT;XLMUSDT;XMRUSDT;BNBUSDT;DOGEUSDT;BANDUSDT;SUSHIUSDT;SOLUSDT";
			//StartDateTextBox.Text = "2023-01-01";
			//EndDateTextBox.Text = "2024-12-31";
			BacktestButton_Click(BacktestMacro3Button, new RoutedEventArgs());
			//BacktestButton_Click(BacktestMacroASButton, new RoutedEventArgs());


			/*
			 * 
			;YFIUSDT;TRBUSDT;SUSHIUSDT;EGLDUSDT;SOLUSDT;UNIUSDT;AVAXUSDT;ENJUSDT;KSMUSDT;AAVEUSDT;RSRUSDT;LRCUSDT;ZENUSDT;GRTUSDT;SANDUSDT;COTIUSDT;HBARUSDT;MTLUSDT;BTCDOMUSDT;MASKUSDT;ARUSDT;LPTUSDT;ENSUSDT;DUSKUSDT;IMXUSDT;API3USDT;APEUSDT;WOOUSDT;JASMYUSDT;OPUSDT;INJUSDT;LDOUSDT;ICPUSDT;APTUSDT;QNTUSDT;FETUSDT;FXSUSDT;HIGHUSDT;ASTRUSDT;PHBUSDT;GMXUSDT;CFXUSDT;STXUSDT;ACHUSDT;SSVUSDT;CKBUSDT;LQTYUSDT;USDCUSDT;IDUSDT;JOEUSDT;HFTUSDT;XVSUSDT;BLURUSDT;SUIUSDT;NMRUSDT;XVGUSDT;WLDUSDT;PENDLEUSDT;ARKMUSDT;AGLDUSDT;DODOXUSDT;BNTUSDT;OXTUSDT;BIGTIMEUSDT;RIFUSDT;POLYXUSDT;TIAUSDT;CAKEUSDT;TWTUSDT;ORDIUSDT;STEEMUSDT;ILVUSDT;KASUSDT;BEAMXUSDT;PYTHUSDT;SUPERUSDT;USTCUSDT;ONGUSDT;ETHWUSDT;JTOUSDT;AUCTIONUSDT;ACEUSDT;MOVRUSDT;NFPUSDT
			 * */
		}

		public record SequenceSet(string EntrySequence, string ExitSequence);

		public List<string> GenerateSequences(int maxLength)
		{
			var result = new List<string>();

			void Backtrack(string current, int depth)
			{
				if (depth > maxLength)
				{
					return;
				}

				if (current.Length > 0)
				{
					result.Add(current);
				}

				Backtrack(current + "U", depth + 1);
				Backtrack(current + "D", depth + 1);
			}

			Backtrack("", 0);

			return result;
		}

		public void For(decimal start, decimal end, decimal step, Action<decimal> action)
		{
			for (decimal i = start; i <= end; i += step)
				action(i);
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
				interval = forceInterval;
				//interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
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

					case "ALL MACRO":
						worker.DoWork += Worker_DoWorkMacroAllSymbol;
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
					backtester.Init(chartPacks, interval);
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
					$"{symbols[0]} +{symbols.Length - 1},{interval},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

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

				// BTCUSDT;BNBUSDT;SUSHIUSDT;ZECUSDT;BAKEUSDT



				//For(1.2m, 1.2m, 0.1m, a1 =>
				//{
				//For(7.0m, 7.0m, 0.5m, a2 =>
				//{
				//For(5.5m, 5.5m, 0.5m, a3 =>
				//	{
				//	For(0.5, 0.5, 0.5, a4 =>
				//		{
				//		For(10, 10, 5, a5 =>
				//			{

				//var entrySeqs = GenerateSequences(6);
				//var exitSeqs = GenerateSequences(4);

				//foreach (var entry in entrySeqs)
				//{
				//	foreach (var exit in exitSeqs)
				//	{
				//		maxActiveDealsType = MaxActiveDealsType.Total;
				//		var leverage = 1;
				//		var maxActiveDeals = 1;
				//		var backtester = new CandleSequence(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
				//		{
				//			IsGeneratePositionHistory = false,
				//			FeeRate = 0.0002m,
				//			entryCondition = entry,
				//			exitCondition = exit,
				//			minBody = 0m
				//		};

				//		backtester.Init(chartPacks);
				//		backtester.Run(startDate.AddDays(10), endDate);

				//		File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
				//			$"{backtester.GetType().Name},{interval.ToIntervalString()},{entry},{exit},{maxActiveDealsType}," +
				//			$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);
				//	}
				//}

				//				});
				//			});
				//		});
				//	});
				//});
				#endregion

				var results = new ConcurrentBag<string>();
				var errors = new ConcurrentBag<string>();

				//var entrySeqs = GenerateSequences(6);
				//var exitSeqs = GenerateSequences(6);

				//Parallel.ForEach(combos, combo =>
				//{
				try
				{
					maxActiveDealsType = MaxActiveDealsType.Total;
					var leverage = 1;
					var maxActiveDeals = 10;

					var backtester = new Cci2(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
					{
						IsGeneratePositionHistory = true,
						IsGenerateDailyHistory = true,
						IsEnableLongPosition = true,
						IsEnableShortPosition = true,
						FeeRate = 0.0003m,
						CciPeriod = 64,
						Deviation = 2.8m
					};

					backtester.Init(chartPacks, interval);
					backtester.Run(startDate.AddDays(10), endDate);

					string result = $"{backtester.GetType().Name},{interval.ToIntervalString()},{maxActiveDealsType}," +
									$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)}," +
									$"{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}";

					results.Add(result);
				}
				catch (Exception ex)
				{

				}
				//});

				var outputPath = MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv");
				File.AppendAllLines(outputPath, results);

				var errorLogPath = MercuryPath.Desktop.Down($"{reportFileName}_Macro_Errors.txt");
				File.WriteAllLines(errorLogPath, errors); // 에러 따로 로그 저장


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
				List<ChartPack> chartPacks = [];
				//For(1.2m, 1.2m, 0.1m, a1 =>
				//{
				//For(4.0m, 4.0m, 0.2m, a2 =>
				//{
				//For(2.5m, 2.5m, 0.5m, a3 =>
				//{
				var randomResults = new List<BacktestR_Result>();
				var r = new SmartRandom();
				var symbols = LocalApi.GetSymbolNames();
				//List<string> symbols = ["BTCUSDT"];
				var period = 365;

				KlineInterval _interval = KlineInterval.OneHour;
				for (int count = 0; count < 50; count++)
				{
					try
					{
						ChartLoader.Charts = [];

						var symbol = r.Next(symbols);
						var symbolListingDate = CryptoSymbol.GetStartDate(symbol).AddDays(1);
						var startDate = r.Next(symbolListingDate, new DateTime(2024, 2, 28));
						var endDate = startDate.AddDays(period);

						// 이미 차트팩을 불러왔으면 스킵
						if (chartPacks.Find(x => x.Symbol == symbol && x.Interval == _interval) is ChartPack existingChartPack)
						{

						}
						else
						{
							var chartPack = ChartLoader.InitCharts(symbol, _interval, startDate, endDate);
							//var chartPack = ChartLoader.InitCharts(symbol, interval, new DateTime(2019, 9, 8), new DateTime(2025, 3, 20));
							if (chartPack.Charts.Count <= 0)
							{
								continue;
							}
							chartPacks.Add(chartPack);
						}

						maxActiveDealsType = MaxActiveDealsType.Total;
						var leverage = 1;
						var maxActiveDeals = 1;
						var backtester = new Cci1(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
						{
							IsGeneratePositionHistory = false,
							IsGenerateDailyHistory = false,
							FeeRate = 0.0003m
						};

						backtester.Init(chartPacks, _interval);
						backtester.Run(startDate.AddDays(10), endDate);

						File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
							$"{symbol},{backtester.GetType().Name},{_interval.ToIntervalString()},{maxActiveDealsType}," +
							$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

						randomResults.Add(new BacktestR_Result(backtester.Win, backtester.Lose, backtester.EstimatedMoney, backtester.Mdd, backtester.ResultPerRisk));
					}
					catch
					{
					}
				}

				var totalWin = randomResults.Sum(x => x.Win);
				var totalLose = randomResults.Sum(x => x.Lose);
				var averageEstimatedMoney = randomResults.Average(x => x.EstimatedMoney);
				var maxMdd = randomResults.Max(x => x.Mdd);
				var averageResultPerRisk = randomResults.Average(x => x.ResultPerRisk);

				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									$"{totalWin},{totalLose},{(totalWin + totalLose > 0 ? (decimal)totalWin / (totalWin + totalLose) : 0).Round(4):P}," +
									$"{averageEstimatedMoney.Round(0)},{maxMdd.Round(4):P},{averageResultPerRisk.Round(4)}" + Environment.NewLine);
				//});
				//});
				//});


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

		private void Worker_DoWorkMacroAllSymbol(object? sender, DoWorkEventArgs e)
		{
			try
			{
				var symbols = LocalApi.GetSymbolNames().Where(x => x.EndsWith("USDT")).ToList();
				symbols.Remove("BNXUSDT");
				symbols.Remove("BTCSTUSDT");
				symbols.Remove("TLMUSDT");
				//symbols = [.. symbols.Take(20)];

				for (int p = 64; p <= 64; p += 32)
				{
					for (decimal d = 2.8m; d <= 2.8m; d += 0.1m)
					{
						var results = new List<BacktestR_Result>();

						foreach (var symbol in symbols)
						{
							try
							{
								ChartLoader.Charts = [];

								var symbolListingDate = CryptoSymbol.GetStartDate(symbol).AddDays(1);
								var symbolEndDate = CryptoSymbol.GetEndDate(symbol);

								if (symbolEndDate - symbolListingDate < TimeSpan.FromDays(365))
								{
									continue;
								}

								var chartPack = ChartLoader.InitCharts(symbol, interval, symbolListingDate, symbolEndDate);
								if (chartPack.Charts.Count <= 0)
								{
									continue;
								}

								maxActiveDealsType = MaxActiveDealsType.Total;
								var leverage = 1;
								var maxActiveDeals = 3;
								var backtester = new Cci2(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
								{
									IsGeneratePositionHistory = false,
									IsGenerateDailyHistory = false,
									IsEnableLongPosition = true,
									IsEnableShortPosition = true,
									FeeRate = 0.0003m,
									CciPeriod = p,
									Deviation = d
								};

								backtester.Init([chartPack], interval);
								backtester.Run(symbolListingDate.AddDays(10), symbolEndDate);

								File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
									$"{symbol},{backtester.GetType().Name},{interval.ToIntervalString()},{maxActiveDealsType}," +
									$"{maxActiveDeals},{leverage},{backtester.Win},{backtester.Lose},{backtester.WinRate.Round(2)},{backtester.EstimatedMoney.Round(0)},{backtester.Mdd.Round(4):P},{backtester.ResultPerRisk.Round(4)}" + Environment.NewLine);

								results.Add(new BacktestR_Result(backtester.Win, backtester.Lose, backtester.EstimatedMoney, backtester.Mdd, backtester.ResultPerRisk));
							}
							catch
							{

							}

							GC.Collect();
							GC.WaitForPendingFinalizers();
						}

						var totalWin = results.Sum(x => x.Win);
						var totalLose = results.Sum(x => x.Lose);
						var averageEstimatedMoney = results.Average(x => x.EstimatedMoney);
						var maxMdd = results.Max(x => x.Mdd);
						var averageResultPerRisk = results.Average(x => x.ResultPerRisk);


						File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_Macro.csv"),
											$"{p},{d.Round(1)},{totalWin},{totalLose},{(totalWin + totalLose > 0 ? (decimal)totalWin / (totalWin + totalLose) : 0).Round(4):P}," +
											$"{averageEstimatedMoney.Round(0)},{maxMdd.Round(4):P},{averageResultPerRisk.Round(4)}" + Environment.NewLine);
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
