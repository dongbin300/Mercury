using Binance.Net.Enums;

using MarinaX.Utils;

using MarinerX.Apis;
using MarinerX.Bots;
using MarinerX.Commas.Noises;
using MarinerX.Commas.Parameters;
using MarinerX.Deals;
using MarinerX.Indicators;
using MarinerX.Markets;
using MarinerX.Utils;
using MarinerX.Views;

using Mercury;

using MercuryTradingModel.Extensions;
using MercuryTradingModel.IO;
using MercuryTradingModel.Maths;
using MercuryTradingModel.TradingModels;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Windows;
using System.Windows.Forms;

using ChartLoader = MarinerX.Charts.ChartLoader;


using MessageBox = System.Windows.MessageBox;

namespace MarinerX
{
	internal class TrayMenu
	{
		private NotifyIcon trayIcon;
		private static ContextMenuStrip menuStrip = new();
		private static ProgressView progressView = new();
		private static ProgressView[] progressViews = new ProgressView[80];
		private string iconFileName = "Resources/Images/chart2.ico";
		private Image iconImage;
		private List<BackTestTmFile> tmBackTestFiles = new();
		private List<string> tmMockTradeFileNames = new();
		private List<string> tmRealTradeFileNames = new();
		private List<string> backTestResultFileNames = new();
		private List<string> symbolNames = new();
		private PositionMonitorView positionMonitorView = new();
		private BalanceMonitorView balanceMonitorView = new();
		private QuoteMonitorView quoteMonitorView = new();

		public TrayMenu()
		{
			symbolNames = LocalStorageApi.SymbolNames;

			iconImage = Image.FromFile(iconFileName);

			trayIcon = new NotifyIcon
			{
				Icon = new Icon(iconFileName),
				Text = $"MarinerX By Gaten",
				Visible = true,
			};

			var watcher = new FileSystemWatcher
			{
				Path = TradingModelPath.InspectedDirectory,
				NotifyFilter = NotifyFilters.FileName,
				Filter = "*.json"
			};
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.Created += new FileSystemEventHandler(OnChanged);
			watcher.Deleted += new FileSystemEventHandler(OnChanged);
			watcher.EnableRaisingEvents = true;

			progressView = new ProgressView();
			progressView.Hide();

			for (int i = 0; i < progressViews.Length; i++)
			{
				progressViews[i] = new ProgressView(i * 15, 0, (int)SystemParameters.PrimaryScreenWidth, 15);
				progressViews[i].Hide();
			}

			RefreshTmFile();
			RefreshMenu();
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			RefreshTmFile();
			RefreshMenu();
		}

		private void RefreshTmFile()
		{
			tmBackTestFiles = Directory.GetFiles(TradingModelPath.InspectedBackTestDirectory).Select(x => new BackTestTmFile(x)).ToList();
			tmMockTradeFileNames = Directory.GetFiles(TradingModelPath.InspectedMockTradeDirectory).ToList();
			tmRealTradeFileNames = Directory.GetFiles(TradingModelPath.InspectedRealTradeDirectory).ToList();
			backTestResultFileNames = Directory.GetFiles(PathUtil.Base.Down("MarinerX"), "*.csv").ToList();
		}

		public void RefreshMenu()
		{
			symbolNames.Sort();

			menuStrip = new ContextMenuStrip();
			menuStrip.Items.Add(new ToolStripMenuItem("MarinerX By Gaten", iconImage));
			menuStrip.Items.Add(new ToolStripSeparator());

			var menu1 = new ToolStripMenuItem("데이터 수집");
			menu1.DropDownItems.Add("Binance 현재가 데이터 수집", null, new EventHandler(GetBinancePriceDataEvent));
			menu1.DropDownItems.Add("Binance 심볼 데이터 수집", null, new EventHandler(GetBinanceSymbolDataEvent));
			menu1.DropDownItems.Add("Binance 1분봉 데이터 수집", null, new EventHandler(GetBinanceCandleDataEvent));
			menu1.DropDownItems.Add("Binance 1일봉 데이터 추출", null, new EventHandler(Extract1DCandleEvent));
			menu1.DropDownItems.Add("Binance 5분봉 데이터 추출", null, new EventHandler(Extract5mCandleEvent));
			menu1.DropDownItems.Add("Binance 15분봉 데이터 추출", null, new EventHandler(Extract15mCandleEvent));
			menu1.DropDownItems.Add("Binance 30분봉 데이터 추출", null, new EventHandler(Extract30mCandleEvent));
			menu1.DropDownItems.Add("Binance 1시간봉 데이터 추출", null, new EventHandler(Extract1hCandleEvent));
			menu1.DropDownItems.Add(new ToolStripSeparator());
			menu1.DropDownItems.Add("Binance 5분봉 지표값 추출", null, new EventHandler(Extract5mIndicatorTs1Event));
			menu1.DropDownItems.Add("Binance 15분봉 지표값 추출", null, new EventHandler(Extract15mIndicatorTs1Event));
			menu1.DropDownItems.Add("Binance 30분봉 지표값 추출", null, new EventHandler(Extract30mIndicatorTs1Event));
			menu1.DropDownItems.Add("Binance 1시간봉 지표값 추출", null, new EventHandler(Extract1hIndicatorTs1Event));
			menu1.DropDownItems.Add(new ToolStripSeparator());
			menu1.DropDownItems.Add("Binance 1분봉 데이터 체크", null, new EventHandler(GetBinanceCandleDataCheckEvent));
			menu1.DropDownItems.Add("Binance 1분봉 매뉴얼 데이터 수집", null, new EventHandler(GetBinanceCandleDataManualEvent));
			menu1.DropDownItems.Add(new ToolStripSeparator());
			menu1.DropDownItems.Add("Binance 가격 데이터 추출", null, new EventHandler(ExtractPriceEvent));
			menuStrip.Items.Add(menu1);

			var menu2 = new ToolStripMenuItem("데이터 로드");
			var menu21 = new ToolStripMenuItem("1분봉 데이터 로드");
			var menu211 = new ToolStripMenuItem("A-D");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'A' && s[0] <= 'D'))
			{
				menu211.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadChartDataEvent(sender, e, symbolName, KlineInterval.OneMinute))));
			}
			var menu212 = new ToolStripMenuItem("E-N");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'E' && s[0] <= 'N'))
			{
				menu212.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadChartDataEvent(sender, e, symbolName, KlineInterval.OneMinute))));
			}
			var menu213 = new ToolStripMenuItem("O-Z");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'O' && s[0] <= 'Z'))
			{
				menu213.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadChartDataEvent(sender, e, symbolName, KlineInterval.OneMinute))));
			}
			var menu22 = new ToolStripMenuItem("5분봉 데이터 로드");
			var menu221 = new ToolStripMenuItem("A-D");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'A' && s[0] <= 'D'))
			{
				menu221.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadChartDataEvent(sender, e, symbolName, KlineInterval.FiveMinutes))));
			}
			var menu222 = new ToolStripMenuItem("E-N");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'E' && s[0] <= 'N'))
			{
				menu222.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadChartDataEvent(sender, e, symbolName, KlineInterval.FiveMinutes))));
			}
			var menu223 = new ToolStripMenuItem("O-Z");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'O' && s[0] <= 'Z'))
			{
				menu223.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadChartDataEvent(sender, e, symbolName, KlineInterval.FiveMinutes))));
			}
			var menu23 = new ToolStripMenuItem("거래 데이터 로드");
			var menu231 = new ToolStripMenuItem("A-D");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'A' && s[0] <= 'D'))
			{
				menu231.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadTradeDataEvent(sender, e, symbolName))));
			}
			var menu232 = new ToolStripMenuItem("E-N");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'E' && s[0] <= 'N'))
			{
				menu232.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadTradeDataEvent(sender, e, symbolName))));
			}
			var menu233 = new ToolStripMenuItem("O-Z");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'O' && s[0] <= 'Z'))
			{
				menu233.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadTradeDataEvent(sender, e, symbolName))));
			}
			var menu24 = new ToolStripMenuItem("가격 데이터 로드");
			var menu241 = new ToolStripMenuItem("A-D");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'A' && s[0] <= 'D'))
			{
				menu241.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadPriceDataEvent(sender, e, symbolName))));
			}
			var menu242 = new ToolStripMenuItem("E-N");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'E' && s[0] <= 'N'))
			{
				menu242.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadPriceDataEvent(sender, e, symbolName))));
			}
			var menu243 = new ToolStripMenuItem("O-Z");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'O' && s[0] <= 'Z'))
			{
				menu243.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, new EventHandler((sender, e) => LoadPriceDataEvent(sender, e, symbolName))));
			}

			menu21.DropDownItems.Add(menu211);
			menu21.DropDownItems.Add(menu212);
			menu21.DropDownItems.Add(menu213);
			menu22.DropDownItems.Add(menu221);
			menu22.DropDownItems.Add(menu222);
			menu22.DropDownItems.Add(menu223);
			menu23.DropDownItems.Add(menu231);
			menu23.DropDownItems.Add(menu232);
			menu23.DropDownItems.Add(menu233);
			menu24.DropDownItems.Add(menu241);
			menu24.DropDownItems.Add(menu242);
			menu24.DropDownItems.Add(menu243);
			menu2.DropDownItems.Add(menu21);
			menu2.DropDownItems.Add(menu22);
			menu2.DropDownItems.Add(menu23);
			menu2.DropDownItems.Add(menu24);
			menuStrip.Items.Add(menu2);
			menuStrip.Items.Add(new ToolStripSeparator());

			menuStrip.Items.Add(new ToolStripMenuItem("Mercury Editor 열기", null, MercuryEditorOpenEvent));
			menuStrip.Items.Add(new ToolStripMenuItem("Mercury Simple Editor 열기", null, MercurySimpleEditorOpenEvent));
			menuStrip.Items.Add(new ToolStripSeparator());

			var menu4 = new ToolStripMenuItem("백테스트");
			foreach (var file in tmBackTestFiles)
			{
				menu4.DropDownItems.Add(new ToolStripMenuItem(file.MenuString, null, BackTestBotRunEvent, file.ToString() + "|+|false"));
			}
			var menu41 = new ToolStripMenuItem("백테스트 차트");
			foreach (var file in tmBackTestFiles)
			{
				menu41.DropDownItems.Add(new ToolStripMenuItem(file.MenuString, null, BackTestBotRunEvent, file.ToString() + "|+|true"));
			}
			var menu42 = new ToolStripMenuItem("백테스트 결과");
			foreach (var file in backTestResultFileNames)
			{
				menu42.DropDownItems.Add(new ToolStripMenuItem(file, null, BackTestResultViewEvent));
			}
			menuStrip.Items.Add(menu4);
			menuStrip.Items.Add(menu41);
			menuStrip.Items.Add(menu42);
			menuStrip.Items.Add(new ToolStripMenuItem("그리드 백테스트", null, GridBackTestEvent));
			menuStrip.Items.Add(new ToolStripSeparator());

			menuStrip.Items.Add(new ToolStripMenuItem("실전매매 봇", null, RealTradeBotEvent));
			menuStrip.Items.Add(new ToolStripSeparator());

			var menu5 = new ToolStripMenuItem("데이터 분석");
			menu5.DropDownItems.Add("벤치마킹", null, new EventHandler(SymbolBenchmarkingEvent));
			menu5.DropDownItems.Add("PNL 분석", null, new EventHandler(PnlAnalysisEvent));
			menuStrip.Items.Add(menu5);

			var menu6 = new ToolStripMenuItem("데이터 모니터링");
			menu6.DropDownItems.Add("검색기", null, new EventHandler(QuoteMonitoringEvent));
			//var menu61 = new ToolStripMenuItem("검색기");
			var menu62 = new ToolStripMenuItem("현재 포지션 모니터링(A-D)");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'A' && s[0] <= 'D'))
			{
				menu62.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, CurrentPositioningEvent, symbolName));
			}
			var menu63 = new ToolStripMenuItem("현재 포지션 모니터링(E-N)");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'E' && s[0] <= 'N'))
			{
				menu63.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, CurrentPositioningEvent, symbolName));
			}
			var menu64 = new ToolStripMenuItem("현재 포지션 모니터링(O-Z)");
			foreach (var symbolName in symbolNames.Where(s => s[0] >= 'O' && s[0] <= 'Z'))
			{
				menu64.DropDownItems.Add(new ToolStripMenuItem(symbolName, null, CurrentPositioningEvent, symbolName));
			}
			//menu6.DropDownItems.Add(menu61);
			menu6.DropDownItems.Add(menu62);
			menu6.DropDownItems.Add(menu63);
			menu6.DropDownItems.Add(menu64);
			menu6.DropDownItems.Add("모니터링 종료", null, new EventHandler(CurrentPositioningEndEvent));
			menu6.DropDownItems.Add("현재 자산 모니터링", null, new EventHandler(CurrentBalanceEvent));
			menu6.DropDownItems.Add("현재 자산 모니터링 종료", null, new EventHandler(CurrentBalanceEndEvent));
			menuStrip.Items.Add(menu6);
			menuStrip.Items.Add(new ToolStripSeparator());

			var menu7 = new ToolStripMenuItem("테스트");
			menu7.DropDownItems.Add(new ToolStripMenuItem("바이낸스 API 통신 테스트", null, BinanceApiCommTestEvent));
			menu7.DropDownItems.Add(new ToolStripSeparator());
			menu7.DropDownItems.Add(new ToolStripMenuItem("RI Histogram", null, RiHistogramEvent));
			menu7.DropDownItems.Add(new ToolStripMenuItem("Run Back Test Flask", null, RunBackTestFlaskEvent));
			menu7.DropDownItems.Add(new ToolStripMenuItem("Run Back Test Flask Multi", null, RunBackTestFlaskMultiEvent));
			menu7.DropDownItems.Add(new ToolStripMenuItem("Significant Rise and Fall", null, SignificantRiseAndFallRatioEvent));
			menuStrip.Items.Add(menu7);

			var menu8 = new ToolStripMenuItem("3Commas 테스트");
			menu8.DropDownItems.Add(new ToolStripMenuItem("RSI", null, CommasRsiEvent));
			menu8.DropDownItems.Add(new ToolStripMenuItem("RSI AI", null, CommasRsiAiEvent));
			menuStrip.Items.Add(menu8);

			var menu9 = new ToolStripMenuItem("매크로");
			menu9.DropDownItems.Add(new ToolStripMenuItem("AggTrades 데이터 수집", null, MacroGetAggTradesEvent));
			menuStrip.Items.Add(menu9);
			menuStrip.Items.Add(new ToolStripSeparator());

			menuStrip.Items.Add(new ToolStripMenuItem("종료", null, Exit));

			menuStrip.Items[0].Enabled = false;
			trayIcon.ContextMenuStrip = menuStrip;
		}

		#region 데이터 수집
		public static void GetBinancePriceDataEvent(object? sender, EventArgs e)
		{
			try
			{
				var prices = BinanceRestApi.GetFuturesPrices().OrderBy(x => x.Symbol);
				var data = string.Join(Environment.NewLine, prices.Select(x => x.Symbol + "," + x.Price));

				System.Windows.Clipboard.SetText(data);
				MessageBox.Show(data);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void GetBinanceSymbolDataEvent(object? sender, EventArgs e)
		{
			try
			{
				var symbolNames = BinanceRestApi.GetFuturesSymbolNames();
				File.WriteAllLines(PathUtil.BinanceFuturesData.Down($"symbol_{DateTime.Now:yyyy-MM-dd}.txt"), symbolNames);

				var symbolData = BinanceRestApi.GetFuturesSymbols();
				symbolData.SaveCsvFile(PathUtil.BinanceFuturesData.Down($"symbol_detail_{DateTime.Now:yyyy-MM-dd}.csv"));

				var bnbPrice = BinanceRestApi.GetCurrentBnbPrice();
				File.WriteAllLines(PathUtil.BinanceFuturesData.Down("BNB.txt"), new List<string> { DateTime.Now.ToStandardFileName(), bnbPrice.ToString() });

				MessageBox.Show("바이낸스 심볼 데이터 수집 완료");

				ProcessUtil.Start(PathUtil.BinanceFuturesData);
				LocalStorageApi.Init();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void GetBinanceCandleDataEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = GetBinanceCandleData
			};
			worker.Start();
		}

		public static void GetBinanceCandleData(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.GetCandleDataFromBinance(worker);
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 1분봉 데이터 수집 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void GetBinanceCandleDataCheckEvent(object? sender, EventArgs e)
		{
			try
			{
				var fileNames = ChartLoader.GetInvalidDataFileNames();
				if (fileNames == null)
				{
					return;
				}

				MessageBox.Show(fileNames.Count + "건\n" + string.Join("\n", fileNames));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void GetBinanceCandleDataManualEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = (worker, obj) =>
				{
					try
					{
						ChartLoader.GetCandleDataFromBinanceManual(worker);
						DispatcherService.Invoke(progressView.Hide);

						MessageBox.Show("바이낸스 1분봉 매뉴얼 데이터 수집 완료");
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message);
					}
				}
			};
			worker.Start();
		}

		public static void ExtractPriceEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = (worker, obj) =>
				{
					try
					{
						ChartLoader.ExtractPricesFromAggregatedTrades("BTCUSDT", worker,
							new DateTime(2023, 1, 1),
							new DateTime(2023, 1, 31));
						DispatcherService.Invoke(progressView.Hide);

						MessageBox.Show("바이낸스 가격 데이터 추출 완료");
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message);
					}
				}
			};
			worker.Start();
		}

		public static void Extract1DCandleEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract1DCandle
			};
			worker.Start();
		}

		public static void Extract1DCandle(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.Extract1DCandle(worker);
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 1일봉 데이터 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract5mCandleEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract5mCandle
			};
			worker.Start();
		}

		public static void Extract5mCandle(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractCandle(worker, KlineInterval.FiveMinutes, "5m");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 5분봉 데이터 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract15mCandleEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract15mCandle
			};
			worker.Start();
		}

		public static void Extract15mCandle(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractCandle(worker, KlineInterval.FifteenMinutes, "15m");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 15분봉 데이터 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract30mCandleEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract30mCandle
			};
			worker.Start();
		}

		public static void Extract30mCandle(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractCandle(worker, KlineInterval.ThirtyMinutes, "30m");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 30분봉 데이터 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract1hCandleEvent(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract1hCandle
			};
			worker.Start();
		}

		public static void Extract1hCandle(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractCandle(worker, KlineInterval.OneHour, "1h");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 1시간봉 데이터 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract5mIndicatorTs1Event(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract5mIndicatorTs1
			};
			worker.Start();
		}

		public static void Extract5mIndicatorTs1(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractIndicatorTs2(worker, KlineInterval.FiveMinutes, "5m");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 5분봉 TS2 지표값 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract15mIndicatorTs1Event(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract15mIndicatorTs1
			};
			worker.Start();
		}

		public static void Extract15mIndicatorTs1(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractIndicatorTs2(worker, KlineInterval.FifteenMinutes, "15m");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 15분봉 TS2 지표값 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract30mIndicatorTs1Event(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract30mIndicatorTs1
			};
			worker.Start();
		}

		public static void Extract30mIndicatorTs1(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractIndicatorTs2(worker, KlineInterval.ThirtyMinutes, "30m");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 30분봉 TS2 지표값 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void Extract1hIndicatorTs1Event(object? sender, EventArgs e)
		{
			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = Extract1hIndicatorTs1
			};
			worker.Start();
		}

		public static void Extract1hIndicatorTs1(Worker worker, object? obj)
		{
			try
			{
				ChartLoader.ExtractIndicatorTs2(worker, KlineInterval.OneHour, "1h");
				DispatcherService.Invoke(progressView.Hide);

				MessageBox.Show("바이낸스 1시간봉 TS2 지표값 추출 완료");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 데이터 로드
		record PeriodChartDataType(string symbol, KlineInterval interval, DateTime startDate, DateTime endDate);
		record ChartDataType(string symbol, KlineInterval interval, bool isExternal);
		record TradeDataType(string symbol, bool isExternal);
		record PriceDataType(string symbol, bool isExternal);

		public static void LoadChartDataEvent(object? sender, EventArgs e, string symbol, KlineInterval interval, bool external = false)
		{
			if (!external)
			{
				progressView.Show();
			}
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = LoadChartData,
				Arguments = new ChartDataType(symbol, interval, external)
			};
			if (external)
			{
				worker.Start().Wait();
			}
			else
			{
				worker.Start();
			}
		}

		public static void LoadChartData(Worker worker, object? obj)
		{
			try
			{
				if (obj is not ChartDataType chartDataType)
				{
					return;
				}
				ChartLoader.InitCharts(chartDataType.symbol, chartDataType.interval, worker);
				if (!chartDataType.isExternal)
				{
					DispatcherService.Invoke(progressView.Hide);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void LoadTradeDataEvent(object? sender, EventArgs e, string symbol, bool external = false)
		{
			if (!external)
			{
				progressView.Show();
			}
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = LoadTradeData,
				Arguments = new TradeDataType(symbol, external)
			};
			if (external)
			{
				worker.Start().Wait();
			}
			else
			{
				worker.Start();
			}
		}

		public static void LoadTradeData(Worker worker, object? obj)
		{
			try
			{
				if (obj is not TradeDataType tradeDataType)
				{
					return;
				}
				ChartLoader.InitTrades(tradeDataType.symbol, worker);
				if (!tradeDataType.isExternal)
				{
					DispatcherService.Invoke(progressView.Hide);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void LoadPriceDataEvent(object? sender, EventArgs e, string symbol, bool external = false)
		{
			if (!external)
			{
				progressView.Show();
			}
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = LoadPriceData,
				Arguments = new PriceDataType(symbol, external)
			};
			if (external)
			{
				worker.Start().Wait();
			}
			else
			{
				worker.Start();
			}
		}

		public static void LoadPriceData(Worker worker, object? obj)
		{
			try
			{
				if (obj is not PriceDataType priceDataType)
				{
					return;
				}
				ChartLoader.InitPrices(priceDataType.symbol, worker);
				if (!priceDataType.isExternal)
				{
					DispatcherService.Invoke(progressView.Hide);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region Mercury Editor
		private void MercuryEditorOpenEvent(object? sender, EventArgs e)
		{
			try
			{
				ProcessUtil.Start("MercuryEditor.exe");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void MercurySimpleEditorOpenEvent(object? sender, EventArgs e)
		{
			try
			{
				ProcessUtil.Start("MercuryEditor.exe", "simple");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 백테스트
		record BackTestParameter(MercuryBackTestTradingModel? model, bool isShowChart);

		private void BackTestBotRunEvent(object? sender, EventArgs e)
		{
			if (sender is not ToolStripMenuItem menuItem)
			{
				return;
			}

			var menuNameSegments = menuItem.Name.Split("|+|");
			var jsonString = File.ReadAllText(menuNameSegments[0]);
			var isShowChart = bool.Parse(menuNameSegments[3]);
			var result = JsonConvert.DeserializeObject<MercuryBackTestTradingModel>(jsonString, new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				NullValueHandling = NullValueHandling.Ignore
			});

			progressView.Show();
			var worker = new Worker()
			{
				ProgressBar = progressView.ProgressBar,
				Action = BackTestBotRun,
				Arguments = new BackTestParameter(result, isShowChart)
			};
			worker.Start();
		}

		public static void BackTestBotRun(Worker worker, object? obj)
		{
			BackTestBot? bot = default!;
			try
			{
				if (obj is not BackTestParameter param)
				{
					DispatcherService.Invoke(progressView.Hide);
					return;
				}

				if (param.model == null)
				{
					throw new Exception("BackTest Trading Model Null");
				}

				bot = new BackTestBot(param.model, worker, param.isShowChart);
				var result = bot.Run();
				DispatcherService.Invoke(progressView.Hide);

				if (result.Count == 0)
				{
					throw new Exception("No Trading!!");
				}

				var path = PathUtil.Base.Down("MarinerX", $"BackTest_{DateTime.Now.ToStandardFileName()}.csv");
				result.SaveCsvFile(path);

				DispatcherService.Invoke(() =>
				{
					var historyView = new BackTestTradingHistoryView();
					historyView.Init(result);
					historyView.Show();
				});

				if (param.isShowChart)
				{
					DispatcherService.Invoke(bot.ChartViewer.Show);
				}
				else
				{
					ProcessUtil.Start(path);
				}
			}
			catch
			{
			}
		}

		public static void BackTestResultViewEvent(object? sender, EventArgs e)
		{
			if (sender is not ToolStripMenuItem item)
			{
				return;
			}

			var fileName = item.Text;
			var historyView = new BackTestTradingHistoryView();
			historyView.Init(fileName);
			historyView.Show();
		}

		public static void GridBackTestEvent(object? sender, EventArgs e)
		{
			var view = new GridBotBackTesterView();
			view.Show();
		}
		#endregion

		#region 실전매매 봇
		private void RealTradeBotEvent(object? sender, EventArgs e)
		{
			try
			{
				var realTradeBotView = new RealTradeBotView();
				realTradeBotView.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 데이터 분석
		private void SymbolBenchmarkingEvent(object? sender, EventArgs e)
		{
			try
			{
				BinanceMarket.Init();

				var benchmarkView = new SymbolBenchmarkingView();
				benchmarkView.Init(BinanceMarket.Benchmarks);
				benchmarkView.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void PnlAnalysisEvent(object? sender, EventArgs e)
		{
			try
			{
				var pnlAnalysisView = new PnlAnalysisView();
				pnlAnalysisView.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 데이터 모니터링
		private void QuoteMonitoringEvent(object? sender, EventArgs e)
		{
			try
			{
				quoteMonitorView.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CurrentPositioningEvent(object? sender, EventArgs e)
		{
			try
			{
				if (sender is not ToolStripMenuItem menuItem)
				{
					return;
				}

				var symbol = menuItem.Name;
				var interval = 3;

				positionMonitorView.Init(symbol, interval);
				positionMonitorView.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CurrentPositioningEndEvent(object? sender, EventArgs e)
		{
			try
			{
				positionMonitorView.Hide();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CurrentBalanceEvent(object? sender, EventArgs e)
		{
			try
			{
				LocalStorageApi.GetSeed();
				balanceMonitorView.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CurrentBalanceEndEvent(object? sender, EventArgs e)
		{
			try
			{
				balanceMonitorView.Hide();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 테스트
		private void BinanceApiCommTestEvent(object? sender, EventArgs e)
		{
			try
			{
				var result = BinanceRestApi.Test();
				MessageBox.Show(result.ServerTime + result.TimeZone);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void RiHistogramEvent(object? sender, EventArgs e)
		{
			try
			{
				IndicatorHistogram.GetRiHistogram("BTCUSDT", KlineInterval.FiveMinutes);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void RunBackTestFlaskEvent(object? sender, EventArgs e)
		{
			try
			{
				progressView.Show();
				var worker = new Worker()
				{
					ProgressBar = progressView.ProgressBar,
					Action = BackTestFlaskRun
				};
				worker.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void BackTestFlaskRun(Worker worker, object? obj)
		{
			try
			{
				var flask = new BackTestFlask(worker);
				var result = flask.Run(100000, "BTCUSDT", KlineInterval.FiveMinutes, new DateTime(2022, 11, 22, 0, 0, 0), TimeSpan.FromDays(3), 0.5, 0.5m);

				DispatcherService.Invoke(progressView.Hide);

				if (result == null)
				{
					throw new Exception("Back Test No Trading!!!");
				}

				var path = PathUtil.Base.Down("MarinerX", $"BackTestFlask_{DateTime.Now.ToStandardFileName()}.csv");
				result.SaveCsvFile(path);

				DispatcherService.Invoke(() =>
				{
					var historyView = new BackTestTradingHistoryView();
					historyView.Init(result);
					historyView.Show();
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void RunBackTestFlaskMultiEvent(object? sender, EventArgs e)
		{
			try
			{
				for (int i = 0; i < 8; i++)
				{
					for (int j = 0; j < 10; j++)
					{
						var pv = progressViews[i * 10 + j];

						pv.Show();
						var worker = new Worker()
						{
							ProgressBar = pv.ProgressBar,
							Action = BackTestFlaskMultiRun,
							Arguments = new _temp_bb(0.3 + 0.1 * i, 0.1m + 0.1m * j)
						};
						worker.Start();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		record _temp_bb(double bw, decimal pr);

		public static void BackTestFlaskMultiRun(Worker worker, object? obj)
		{
			try
			{
				if (obj is not _temp_bb tb)
				{
					return;
				}

				var bandwidth = tb.bw;
				var profitRoe = tb.pr;

				var flask = new BackTestFlask(worker);
				var result = flask.Run(100000, "XRPUSDT", KlineInterval.FiveMinutes, new DateTime(2022, 10, 1, 0, 0, 0), TimeSpan.FromDays(30), bandwidth, profitRoe);

				DispatcherService.Invoke(Window.GetWindow(worker.ProgressBar).Hide);

				if (result == null)
				{
					throw new Exception("Back Test No Trading!!!");
				}

				var path = PathUtil.Base.Down("MarinerX", $"BackTestFlask_{DateTime.Now.ToStandardFileName()}_b{bandwidth}_r{profitRoe}.csv");
				result.SaveCsvFile(path);

				//DispatcherService.Invoke(() =>
				//{
				//    var historyView = new BackTestTradingHistoryView();
				//    historyView.Init(result);
				//    historyView.Show();
				//});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void SignificantRiseAndFallRatioEvent(object? sender, EventArgs e)
		{
			try
			{
				var data = LocalStorageApi.GetOneDayQuotes("BTCUSDT");
				var significantCount = data.Count(x => Math.Abs(StockUtil.Roe(MercuryTradingModel.Enums.PositionSide.Long, x.Open, x.Close)) >= 4.0m);
				var ratio = (double)significantCount / data.Count * 100;

				MessageBox.Show($"Significant Count: {significantCount} / {data.Count} ({ratio:f2}%)");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 3Commas 테스트
		private void CommasRsiEvent(object? sender, EventArgs e)
		{
			try
			{
				var result = new List<CommasDealManager>();
				//var symbols = LocalStorageApi.SymbolNames;
				var symbols = new List<string> {
//"BCHUSDT",
//"XRPUSDT",
//"EOSUSDT",
//"LTCUSDT",
//"TRXUSDT",
//"ETCUSDT",
//"XLMUSDT",
//"ADAUSDT",
//"XMRUSDT",
//"DASHUSDT",
//"ZECUSDT",
//"XTZUSDT",
//"ATOMUSDT",
//"BNBUSDT",
//"ONTUSDT",
//"IOTAUSDT",
//"BATUSDT",
//"VETUSDT",
//"NEOUSDT",
//"QTUMUSDT",
//"IOSTUSDT",
//"THETAUSDT",
//"ALGOUSDT",
//"ZILUSDT",
//"KNCUSDT",
//"ZRXUSDT",
//"COMPUSDT",
//"OMGUSDT",
//"DOGEUSDT",
//"SXPUSDT",
//"KAVAUSDT",
//"BANDUSDT",
//"RLCUSDT",
//"WAVESUSDT",
//"MKRUSDT",
//"SNXUSDT",
//"DOTUSDT",
//"YFIUSDT",
//"BALUSDT",
//"CRVUSDT",
//"TRBUSDT",
//"RUNEUSDT",
//"SUSHIUSDT",
//"EGLDUSDT",
//"SOLUSDT",
//"ICXUSDT",
//"STORJUSDT",
//"BLZUSDT",
//"UNIUSDT",
//"AVAXUSDT",
//"FTMUSDT",
//"ENJUSDT",
//"FLMUSDT",
//"TOMOUSDT",
//"RENUSDT",
//"KSMUSDT",
//"NEARUSDT",
//"AAVEUSDT",
//"FILUSDT",
//"LRCUSDT",
//"RSRUSDT",
//"MATICUSDT",
//"OCEANUSDT",
//"CVCUSDT",
//"BELUSDT",
//"CTKUSDT",
//"AXSUSDT",
//"ALPHAUSDT",
//"ZENUSDT",
//"SKLUSDT",
//"GRTUSDT",
//"CHZUSDT",
//"SANDUSDT",
//"ANKRUSDT",
//"BTSUSDT",
//"LITUSDT",
//"UNFIUSDT",
//"REEFUSDT",
//"RVNUSDT",
//"SFPUSDT",
//"XEMUSDT",
//"COTIUSDT",
//"CHRUSDT",
//"MANAUSDT",
//"ALICEUSDT",
//"HBARUSDT",
//"ONEUSDT",
//"LINAUSDT",
//"STMXUSDT",
//"DENTUSDT",
//"CELRUSDT",
//"HOTUSDT",
//"MTLUSDT",
//"OGNUSDT",
//"NKNUSDT",
//"SCUSDT",
//"DGBUSDT",
//"BAKEUSDT",
//"GTCUSDT",
//"TLMUSDT",
//"IOTXUSDT",
//"AUDIOUSDT",
//"RAYUSDT",
//"C98USDT",
//"MASKUSDT",
//"ATAUSDT",
//"DYDXUSDT",
//"GALAUSDT",
//"CELOUSDT",
//"ARUSDT",
//"KLAYUSDT",
//"ARPAUSDT",
//"CTSIUSDT",
//"LPTUSDT",
//"ENSUSDT"

//"PEOPLEUSDT",
//"ANTUSDT",
//"ROSEUSDT",
//"DUSKUSDT",
//"FLOWUSDT",
//"IMXUSDT",
//"API3USDT",
//"GMTUSDT",
//"APEUSDT",
//"BNXUSDT",
//"WOOUSDT",
//"JASMYUSDT",
//"DARUSDT",
//"GALUSDT",
//"OPUSDT",
//"INJUSDT",
//"STGUSDT",
//"SPELLUSDT",
//"CVXUSDT",
//"LDOUSDT",
//"ICPUSDT",
//"APTUSDT",
//"QNTUSDT",

//"FETUSDT",
//"FXSUSDT",
//"HOOKUSDT",
//"MAGICUSDT",
//"TUSDT",
//"RNDRUSDT",
//"HIGHUSDT",
//"MINAUSDT",
//"ASTRUSDT",
//"PHBUSDT",
//"AGIXUSDT",
//"GMXUSDT",
//"CFXUSDT",
//"STXUSDT",
//"ACHUSDT",
//"SSVUSDT",
//"CKBUSDT",

                    "TUSDT",


				};

				foreach (var symbol in symbols)
				{
					try
					{
						var interval = KlineInterval.FiveMinutes;
						var startDate = DateTime.Parse("2023-03-01");
						var endDate = DateTime.Parse("2023-06-01");

						// 차트 로드 및 초기화
						ChartLoader.InitChartsByDate(symbol, interval, new Worker(new Views.Controls.TextProgressBar()), startDate, endDate);

						// 차트 진행하면서 매매
						var charts = ChartLoader.GetChartPack(symbol, interval);
						charts.CalculateCommasIndicatorsEveryonesCoin();

						//for (decimal r = 1.0m; r <= 2.0m; r += 0.05m)
						{
							var dealManager = new CommasDealManager(1.75m, 100, 0, 0, 0, 0, 0);
							for (int i = 1; i < charts.Charts.Count; i++)
							{
								dealManager.EvaluateEveryonesCoin(charts.Charts[i], charts.Charts[i - 1]);
							}
							// Set latest chart for UPNL
							dealManager.ChartInfo = charts.Charts[^1];
							result.Add(dealManager);
						}
					}
					catch (FileNotFoundException)
					{
						continue;
					}
				}

				//foreach (var d in result)
				//{
				//    var content = $"{d.ChartInfo.Symbol},{d.TargetRoe},{d.WinCount},{d.LoseCount},{Math.Round(d.WinRate, 2)}" + Environment.NewLine;
				//    File.AppendAllText(PathUtil.Base.Down("EveryonesCoin_Backtest_History5m.csv"), content);
				//}

				//var dealManager = new CommasDealManager(1.75m, 100, 0, 0, 0, 0, 0);
				//{
				//    try
				//    {
				//        var symbol = "ETCUSDT";
				//        var interval = KlineInterval.FiveMinutes;
				//        var startDate = DateTime.Parse("2021-01-01");
				//        var endDate = DateTime.Parse("2023-06-01");

				//        // 차트 로드 및 초기화
				//        ChartLoader.InitChartsByDate(symbol, interval, new Worker(new Views.Controls.TextProgressBar()), startDate, endDate);

				//        // 차트 진행하면서 매매
				//        var charts = ChartLoader.GetChartPack(symbol, interval);
				//        charts.CalculateCommasIndicatorsEveryonesCoin();

				//        for (int i = 1; i < charts.Charts.Count; i++)
				//        {
				//            dealManager.EvaluateEveryonesCoin(charts.Charts[i], charts.Charts[i - 1]);
				//        }
				//        // Set latest chart for UPNL
				//        dealManager.ChartInfo = charts.Charts[^1];
				//    }
				//    catch (FileNotFoundException)
				//    {
				//        //continue;
				//    }
				//}

				//var content = $"{dealManager.TargetRoe},{dealManager.WinCount},{dealManager.LoseCount},{Math.Round(dealManager.WinRate, 2)},{Math.Round(dealManager.TotalIncome, 3)}" + Environment.NewLine;
				//File.AppendAllText(PathUtil.Base.Down("EveryonesCoin_Backtest_History_v3.csv"), content);

				//var etiPlus = result.Where(x => x.EstimatedTotalIncome > 0).ToList();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CommasRsiAiEvent(object? sender, EventArgs e)
		{
			try
			{
				var random = new SmartRandom();
				var result = new List<CommasDealManager>();
				var symbols = LocalStorageApi.SymbolNames;
				var interval = KlineInterval.FiveMinutes;
				var dayCount = 90;
				var baseOrderSize = 100;

				// 초기 파라미터 값
				decimal preEti = 0;
				var targetRoe = new NoisedParameter(new QuadraticNoise(0.5m, 2.5m), 1.0m);
				var safetyOrderSize = new NoisedParameter(new QuadraticNoise(50, 1000), 100);
				var maxSafetyOrderCount = new NoisedParameter(new QuadraticNoise(1, 20), 10);
				var deviation = new NoisedParameter(new QuadraticNoise(1.0m, 10.0m), 2.0m);
				var stepScale = new NoisedParameter(new QuadraticNoise(1.0m, 3.0m), 1.0m);
				var volumeScale = new NoisedParameter(new QuadraticNoise(1.0m, 3.0m), 1.2m);

				for (int i = 0; i < 100; i++)
				{
					var symbol = random.Next(symbols);
					var fileName = random.Next(Directory.GetFiles(PathUtil.BinanceFuturesData.Down("1m", symbol), "*.csv"));
					var startDate = SymbolUtil.GetDate(fileName);
					var symbolEndDate = SymbolUtil.GetEndDate(symbol);
					if ((symbolEndDate - startDate).TotalDays < dayCount)
					{
						continue;
					}
					var endDate = startDate.AddDays(dayCount);

					try
					{
						// 차트 로드 및 초기화
						ChartLoader.InitChartsByDate(symbol, interval, new Worker(new Views.Controls.TextProgressBar()), startDate, endDate);
					}
					catch (FileNotFoundException)
					{
						continue;
					}

					// 차트 진행하면서 매매
					var charts = ChartLoader.GetChartPack(symbol, interval);
					charts.CalculateCommasIndicators();

					// 파라미터 설정
					var dealManager = new CommasDealManager(targetRoe.Value, baseOrderSize, safetyOrderSize.Value, (int)maxSafetyOrderCount.Value, deviation.Value, stepScale.Value, volumeScale.Value);
					foreach (var info in charts.Charts)
					{
						dealManager.Evaluate(info);
					}
					// Set latest chart for UPNL
					dealManager.ChartInfo = charts.Charts[^1];
					result.Add(dealManager);

					// 파라미터 적합도 점수 평가 및 조정
					var etiDiff = dealManager.EstimatedTotalIncome - preEti;
					preEti = dealManager.EstimatedTotalIncome;
					decimal _noise = 0;
					if (etiDiff > 0) // 이전 모델보다 더 높은 수익 : 낮은 Noise로 파라미터 조정
					{
						var noise = new InverseQuadraticNoise(0, 2000);
						_noise = noise.GetNoiseValue(etiDiff) * 0.2m; // Noise = 0 ~ 0.2
						targetRoe.Adjust(_noise);
						safetyOrderSize.Adjust(_noise);
						maxSafetyOrderCount.Adjust(_noise);
						deviation.Adjust(_noise);
						stepScale.Adjust(_noise);
						volumeScale.Adjust(_noise);
					}
					else // 더 낮은 수익 : 높은 Noise로 파라미터 조정
					{
						var noise = new InverseQuadraticNoise(-2000, 0);
						_noise = noise.GetNoiseValue(etiDiff) * 0.8m + 0.2m; // Noise = 0.2 ~ 1.0
						targetRoe.Adjust(_noise);
						safetyOrderSize.Adjust(_noise);
						maxSafetyOrderCount.Adjust(_noise);
						deviation.Adjust(_noise);
						stepScale.Adjust(_noise);
						volumeScale.Adjust(_noise);
					}

					File.AppendAllText(PathUtil.BinanceFuturesData.Down("RSI_AI.txt"),
						$"{DateTime.Now:yyyy-MM-dd HH:mm:ss}, {symbol}, {startDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd}, ETI: {dealManager.EstimatedTotalIncome:F4}, Noise: {_noise:F4}, TargetROE: {targetRoe.Value:F4}, SafetyOrderSize: {safetyOrderSize.Value:F4}, MaxSafetyOrderCount: {(int)maxSafetyOrderCount.Value}, Deviation: {deviation.Value:F4}, StepScale: {stepScale.Value:F4}, VolumeScale: {volumeScale.Value:F4}" + Environment.NewLine);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region 매크로
		private void MacroGetAggTradesEvent(object? sender, EventArgs e)
		{
			try
			{
				var symbol = "XRPUSDT";
				var startTime = new DateTime(2022, 7, 1);
				var currentTime = startTime;
				var endTime = new DateTime(2023, 12, 31);
				using var client = new HttpClient();

				while (currentTime <= endTime)
				{
					var url = $"https://data.binance.vision/data/futures/um/daily/aggTrades/{symbol}/{symbol}-aggTrades-{currentTime:yyyy-MM-dd}.zip";
					var response = client.GetAsync(url).Result;
					if (response.IsSuccessStatusCode)
					{
						using Stream responseStream = response.Content.ReadAsStreamAsync().Result;
						using FileStream fileStream = File.Create(PathUtil.BinanceFuturesData.Down("trade", symbol, $"{currentTime:yyyy-MM-dd}.zip"));
						responseStream.CopyTo(fileStream);
					}
					currentTime = currentTime.AddDays(1);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		#region Exit
		private void Exit(object? sender, EventArgs e)
		{
			Environment.Exit(0);
		}
		#endregion
	}
}
