using TradeBot.Bots;
using TradeBot.Clients;
using TradeBot.Models;
using TradeBot.Systems;
using TradeBot.Views;

using Mercury;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TradeBot
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		DispatcherTimer timer = new();
		DispatcherTimer mockTimer = new();
		DispatcherTimer timer5s = new();
		DispatcherTimer timer1m = new();
		ManagerBot manager = new("매니저 봇", "심볼 모니터링, 포지션 모니터링, 자산 모니터링 등등 전반적인 시스템을 관리하는 봇입니다.");
		LongBot longBot = new("롱 봇", "롱 포지션 매매를 하는 봇입니다.");
		ShortBot shortBot = new("숏 봇", "숏 포지션 매매를 하는 봇입니다.");
		MockBot mockBot = new("모의 봇", "모의 매매를 하는 봇입니다.");

		IEnumerable<BinanceRealizedPnlHistory> todayRealizedPnlHistory = default!;

		decimal balance = -1;
		decimal todayPnl = 0;

		public MainWindow()
		{
			InitializeComponent();
			Init();
		}

		private async void Init()
		{
			if (!Directory.Exists("Logs"))
			{
				Directory.CreateDirectory("Logs");
			}

			Common.LoadSymbolDetail();

			BinanceClients.Init();

			// 봇 히스토리 추가
			Common.AddHistory = (subject, text) =>
			{
				DispatcherService.Invoke(() =>
				{
					var history = new BotHistory(DateTime.Now, subject, text);
					HistoryDataGrid.Items.Add(history);
					Logger.LogHistory(history);
				});
			};

			await manager.GetAllKlines(300).ConfigureAwait(false);
			await manager.StartBinanceFuturesTicker().ConfigureAwait(false);

			timer.Interval = TimeSpan.FromMilliseconds(1000);
			timer.Tick += Timer_Tick;
			timer.Start();

			//timer5s.Interval = TimeSpan.FromSeconds(5);
			//timer5s.Tick += Timer5s_Tick;
			//timer5s.Start();

			timer1m.Interval = TimeSpan.FromMinutes(1);
			timer1m.Tick += Timer1m_Tick;
			timer1m.Start();
		}

		private void Timer1m_Tick(object? sender, EventArgs e)
		{
			try
			{
				if (!Common.IsSound)
				{
					return;
				}

				if (decimal.TryParse(UpperAlarmTextBox.Text, out var upper))
				{
					if (balance > upper)
					{
						Sound.Play("Resources/upper.wav", 0.5);
					}
				}

				if (decimal.TryParse(LowerAlarmTextBox.Text, out var lower))
				{
					if (balance < lower)
					{
						Sound.Play("Resources/lower.wav", 0.5);
					}
				}

				/* 주문 모니터링 - 5분이 넘도록 체결이 안되는 주문 취소 (이 부분은 좀 더 테스트 필요) */
				//await longPosition.MonitorOpenOrderTimeout().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private async void Timer_Tick(object? sender, EventArgs e)
		{
			try
			{
				if (mockBot.IsRunning)
				{
					return;
				}

				/* 포지션 모니터링 */
				await manager.GetBinancePositions().ConfigureAwait(false);
				await manager.GetBinanceOpenOrders().ConfigureAwait(false);
				DispatcherService.Invoke(() =>
				{
					PositionDataGrid.ItemsSource = Common.Positions;
					IndicatorDataGrid.ItemsSource = null;
					IndicatorDataGrid.ItemsSource = Common.PairQuotes.OrderBy(x => x.Symbol);
				});

				/* 자산 모니터링 */
				(var total, var avbl, var bnb) = await manager.GetBinanceBalance();
				DispatcherService.Invoke(() =>
				{
					balance = total;
					BalanceText.Text = $"{total} USDT";
					BnbText.Text = $"{bnb} BNB";
				});

				/* 수익 모니터링 */
				todayRealizedPnlHistory = await manager.GetBinanceTodayRealizedPnlHistory();
				DispatcherService.Invoke(() =>
				{
					if (todayRealizedPnlHistory != null && !todayRealizedPnlHistory.Any(x => x == null))
					{
						todayPnl = Math.Round(todayRealizedPnlHistory.Sum(x => x.RealizedPnl), 3);
						if (todayPnl >= 0)
						{
							TodayPnlText.Foreground = Common.LongColor;
							TodayPnlText.Text = $"+{todayPnl} USDT";
						}
						else
						{
							TodayPnlText.Foreground = Common.ShortColor;
							TodayPnlText.Text = $"{todayPnl} USDT";
						}
					}
				});

				/* 매시 0분 0초에 보고서 작성 */
				if (DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
				{
					Logger.LogReport(total, bnb, todayPnl, longBot.BaseOrderSize, longBot.Leverage, longBot.MaxActiveDeals);
				}

				/* 매시 1분 0초에 [29분후에 모든 미체결주문 취소] 작업 실행 */
				if (DateTime.Now.Minute == 1 && DateTime.Now.Second == 0)
				{
					foreach (var openOrder in Common.OpenOrders)
					{
						await BinanceClients.Api.UsdFuturesApi.Trading.CancelAllOrdersAfterTimeoutAsync(openOrder.Symbol, TimeSpan.FromMinutes(29)).ConfigureAwait(false);
					}
				}

				if (longBot.IsRunning)
				{
					await longBot.Evaluate().ConfigureAwait(false);
				}

				if (shortBot.IsRunning)
				{
					await shortBot.Evaluate().ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private async void Timer5s_Tick(object? sender, EventArgs e)
		{
			try
			{
				/* 주문 검수 */
				if (longBot.IsRunning || shortBot.IsRunning)
				{
					await manager.MonitorOpenOrderClosedDeal().ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		#region Mock
		private void MockBotCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				mockBot.BaseOrderSize = int.Parse(BaseOrderSizeTextBox.Text);
				//longPosition.TargetRoe = decimal.Parse(TargetProfitTextBox.Text);
				mockBot.Leverage = int.Parse(LeverageTextBox.Text);
				mockBot.MaxActiveDeals = int.Parse(MaxActiveDealsTextBox.Text);

				mockTimer.Interval = TimeSpan.FromMilliseconds(1000);
				mockTimer.Tick += async (s, e) =>
				{
					try
					{
						/* 포지션 모니터링 */
						DispatcherService.Invoke(() =>
						{
							PositionDataGrid.ItemsSource = mockBot.Positions;
							//IndicatorDataGrid.ItemsSource = null;
							//IndicatorDataGrid.ItemsSource = Common.PairQuotes.OrderBy(x => x.Symbol);
						});

						/* 자산 모니터링 */
						DispatcherService.Invoke(() =>
						{
							balance = mockBot.Money;
							BalanceText.Text = $"{mockBot.Money.Round(2)} USDT";
							//BnbText.Text = $"{bnb} BNB";
						});

						/* 수익 모니터링 */
						DispatcherService.Invoke(() =>
						{
							var incomes = mockBot.PositionHistory.Sum(x => x.Income);
							if (incomes >= 0)
							{
								TodayPnlText.Foreground = Common.LongColor;
								TodayPnlText.Text = $"+{incomes.Round(2)} USDT";
							}
							else
							{
								TodayPnlText.Foreground = Common.ShortColor;
								TodayPnlText.Text = $"{incomes.Round(2)} USDT";
							}
						});

						/* 매시 0분 0초에 보고서 작성 */
						if (DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
						{
							Logger.LogReport(mockBot.Money, 0, todayPnl, mockBot.BaseOrderSize, mockBot.Leverage, mockBot.MaxActiveDeals);
						}

						if (mockBot.IsRunning)
						{
							await mockBot.Evaluate().ConfigureAwait(false);
						}
					}
					catch (Exception ex)
					{
						Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
					}
				};
				mockTimer.Start();

				Common.AddHistory("Master", "Mock Bot On");
				mockBot.IsRunning = true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void MockBotCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			try
			{
				mockTimer.Stop();

				Common.AddHistory("Master", "Mock Bot Off");
				mockBot.IsRunning = false;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}
		#endregion

		#region Bot Activate
		private void LongBotCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				longBot.BaseOrderSize = int.Parse(BaseOrderSizeTextBox.Text);
				//longPosition.TargetRoe = decimal.Parse(TargetProfitTextBox.Text);
				longBot.Leverage = int.Parse(LeverageTextBox.Text);
				longBot.MaxActiveDeals = int.Parse(MaxActiveDealsTextBox.Text);

				Common.AddHistory("Master", "Long Bot On");
				longBot.IsRunning = true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void LongBotCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			try
			{
				Common.AddHistory("Master", "Long Bot Off");
				longBot.IsRunning = false;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void ShortBotCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				shortBot.BaseOrderSize = int.Parse(BaseOrderSizeTextBox.Text);
				//shortPosition.TargetRoe = decimal.Parse(TargetProfitTextBox.Text);
				shortBot.Leverage = int.Parse(LeverageTextBox.Text);
				shortBot.MaxActiveDeals = int.Parse(MaxActiveDealsTextBox.Text);

				Common.AddHistory("Master", "Short Bot On");
				shortBot.IsRunning = true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void ShortBotCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			try
			{
				Common.AddHistory("Master", "Short Bot Off");
				shortBot.IsRunning = false;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}
		#endregion

		#region Require Asset
		private int CalculateRequireAsset()
		{
			try
			{
				if (MaxActiveDealsTextBox == null || BaseOrderSizeTextBox == null || LeverageTextBox == null)
				{
					return 0;
				}

				return (int)(MaxActiveDealsTextBox.Text.ToInt() * BaseOrderSizeTextBox.Text.ToDecimal() / LeverageTextBox.Text.ToInt());
			}
			catch
			{
				return 0;
			}
		}

		private void BaseOrderSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (RequireAssetText == null)
			{
				return;
			}

			RequireAssetText.Text = $"{CalculateRequireAsset():N} USDT";
		}

		private void LeverageTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (RequireAssetText == null)
			{
				return;
			}

			RequireAssetText.Text = $"{CalculateRequireAsset():N} USDT";
		}

		private void MaxActiveDealsTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (RequireAssetText == null)
			{
				return;
			}

			RequireAssetText.Text = $"{CalculateRequireAsset():N} USDT";
		}
		#endregion


		private void TodayPnlText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			try
			{
				var pnlWindow = new RealizedPnlWindow();
				pnlWindow.Init(todayRealizedPnlHistory);
				pnlWindow.Show();
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}


		private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			var parent = VisualTreeHelper.GetParent(child);

			if (parent == null)
				return null;

			var parentT = parent as T;
			return parentT ?? FindParent<T>(parent);
		}
		public Process? Start(string path)
		{
			ProcessStartInfo info = new()
			{
				FileName = path,
				UseShellExecute = true
			};

			return Process.Start(info);
		}
		private void IndicatorDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
			if (row != null && row.Item is PairQuote pairQuote)
			{
				var symbol = pairQuote.Symbol;
				var url = $"https://www.tradingview.com/chart/g2jIOGTD/?symbol=BINANCE%3A{symbol}.P";
				Start(url);
			}
		}

		private void SoundCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			Common.IsSound = true;
		}

		private void SoundCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			Common.IsSound = false;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (longBot.IsRunning || shortBot.IsRunning)
			{
				if (MessageBox.Show("로봇이 작동 중입니다.\n정말 종료하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					e.Cancel = false;
				}
				else
				{
					e.Cancel = true;
				}
			}
		}

		private void AssetCalculatorButton_Click(object sender, RoutedEventArgs e)
		{
			var calculatorView = new AssetCalculatorWindow();
			calculatorView.Show();
		}
	}
}
