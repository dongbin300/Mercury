using Mercury.Enums;
using Mercury.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using TradeBot.Bots;
using TradeBot.Clients;
using TradeBot.Extensions;
using TradeBot.Models;
using TradeBot.Systems;
using TradeBot.Views;

namespace TradeBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// TODO
    /// 자산 소켓으로 
    /// </summary>
    public partial class MainWindow : Window
	{
		#region Properties
		DispatcherTimer timer = new();
		DispatcherTimer mockTimer = new();
		DispatcherTimer timer2s = new();
		DispatcherTimer timer5m = new();
		ManagerBot manager = new("매니저 봇", "심볼 모니터링, 포지션 모니터링, 자산 모니터링 등등 전반적인 시스템을 관리하는 봇입니다.");
		LongBot longBot = new("롱 봇", "롱 포지션 매매를 하는 봇입니다.");
		ShortBot shortBot = new("숏 봇", "숏 포지션 매매를 하는 봇입니다.");
		MockBot mockBot = new("모의 봇", "모의 매매를 하는 봇입니다.");
		ReportBot reportBot = new("보고서 봇", "보고서 작성을 해주는 봇입니다.");

		//IEnumerable<BinanceRealizedPnlHistory> todayRealizedPnlHistory = default!;

		decimal todayPnl = 0;
		decimal usdt = 0;
		decimal bnb = 0;

		private bool _isFullScreen = false;
		private WindowStyle _previousWindowStyle;
		private ResizeMode _previousResizeMode;
		private double _previousTop;
		private double _previousLeft;
		private double _previousWidth;
		private double _previousHeight;

		string themeColorString = string.Empty;
		string foregroundColorString = string.Empty;
		string backgroundColorString = string.Empty;
		string longColorString = string.Empty;
		string shortColorString = string.Empty;
		#endregion

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

			Common.CheckAdmin();
			if (Common.IsAdmin)
			{
				AdminText.Text = "ⓒ Gaten";
			}

			BaseOrderSizeTextBox.Text = Settings.Default.BaseOrderSize;
			LeverageTextBox.Text = Settings.Default.Leverage;
			MaxActiveDealsTypeComboBox.SelectedIndex = Settings.Default.MaxActiveDealsTypeIndex;
			MaxActiveDealsTextBox.Text = Settings.Default.MaxActiveDeals;
			themeColorString = Settings.Default.ThemeColor;
			if (string.IsNullOrEmpty(themeColorString))
			{
				themeColorString = "#FFFFFFFF";
			}
			foregroundColorString = Settings.Default.ForegroundColor;
			if (string.IsNullOrEmpty(foregroundColorString))
			{
				foregroundColorString = "#FFF1F1F1";
			}
			backgroundColorString = Settings.Default.BackgroundColor;
			if (string.IsNullOrEmpty(backgroundColorString))
			{
				backgroundColorString = "#FF131722";
			}
			longColorString = Settings.Default.LongColor;
			if (string.IsNullOrEmpty(longColorString))
			{
				longColorString = "#FF0ECB81";
			}
			shortColorString = Settings.Default.ShortColor;
			if (string.IsNullOrEmpty(shortColorString))
			{
				shortColorString = "#FFF6465D";
			}
			ThemeColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(themeColorString);
			ForegroundColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(foregroundColorString);
			BackgroundColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(backgroundColorString);
			LongColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(longColorString);
			ShortColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(shortColorString);

			reportBot.Init();
			Common.LoadSymbolDetail();
			Common.LoadBlacklist();

			Common.AddHistory = (subject, text) =>
			{
				DispatcherService.Invoke(() =>
				{
					var history = new BotHistory(DateTime.Now, subject, text);
					HistoryDataGrid.Items.Insert(0, history);
					Logger.LogHistory(history);
				});
			};

			#region 통신 부분 (디버깅 시 주석 처리)
			var commResult = BinanceClients.Init();
			Common.AddHistory("API", commResult);

			await manager.GetAllKlines(10).ConfigureAwait(false);
			await manager.StartBinanceFuturesTicker().ConfigureAwait(false);

			timer.Interval = TimeSpan.FromMilliseconds(1000);
			timer.Tick += Timer_Tick;
			timer.Start();

			timer2s.Interval = TimeSpan.FromSeconds(2);
			timer2s.Tick += Timer2s_Tick;
			timer2s.Start();

			timer5m.Interval = TimeSpan.FromMinutes(5);
			timer5m.Tick += Timer5m_Tick;
			timer5m.Start();

			#endregion

			DispatcherService.Invoke(() =>
			{
				SmartSeedCheckBox.IsChecked = true;
			});
		}

		private void Timer5m_Tick(object? sender, EventArgs e)
		{
			try
			{
				Common.SaveBlacklist();

				var hour = DateTime.Now.Hour;
				var minute = DateTime.Now.Minute;
				var dayOfWeek = DateTime.Now.DayOfWeek;

				/* 매일 오전 8시 53~58분 사이에 스마트 시드 */
				DispatcherService.Invoke(() =>
				{
					if ((SmartSeedCheckBox.IsChecked ?? false) && hour == 8 && minute >= 53 && minute <= 58)
					{
						var todayLeverage = longBot.FixedLeverages[(int)dayOfWeek];
						var todayBaseOrderSize = longBot.MaxActiveDealsType == MaxActiveDealsType.Total ?
						Common.Balance * 0.99m * todayLeverage / longBot.MaxActiveDeals :
						Common.Balance * 0.99m * todayLeverage / longBot.MaxActiveDeals / 2;

						BaseOrderSizeTextBox.Text = ((int)todayBaseOrderSize).ToString();
						LeverageTextBox.Text = todayLeverage.ToString();

						LongBotCheckBox_Checked(sender, new RoutedEventArgs());
						ShortBotCheckBox_Checked(sender, new RoutedEventArgs());
					}
				});

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
				DispatcherService.Invoke(() =>
				{
					TimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					//UtcTimeText.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
				});

				if (mockBot.IsRunning)
				{
					return;
				}

				var minute = DateTime.Now.Minute;
				var second = DateTime.Now.Second;

				/* 매시 0분 0,1,2,6,7,8,12,13,14,...초에 포지션 정리 */
				if (minute == 0 && second % 6 <= 2)
				{
					if (longBot.IsRunning)
					{
						await longBot.EvaluateClose().ConfigureAwait(false);
					}
					if (shortBot.IsRunning)
					{
						await shortBot.EvaluateClose().ConfigureAwait(false);
					}
				}
				/* 매시 0분 3,4,5,9,10,11,15,16,17,...초에 포지션 진입 */
				else if (minute == 0 && second % 6 > 2)
				{
					if (longBot.IsRunning)
					{
						await longBot.EvaluateOpen().ConfigureAwait(false);
					}
					if (shortBot.IsRunning)
					{
						await shortBot.EvaluateOpen().ConfigureAwait(false);
					}
				}

				/* 매시 15분 2초에 모든 미체결주문 취소 */
				if (minute == 15 && second == 0)
				{
					foreach (var openOrder in Common.OpenOrders)
					{
						await BinanceClients.Api.UsdFuturesApi.Trading.CancelAllOrdersAfterTimeoutAsync(openOrder.Symbol, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
						Common.AddHistory("Master", $"Cancel Order {openOrder.Symbol}, {openOrder.Side}, {openOrder.FilledString}");
					}
				}

				/* 매시 0분 0초에 보고서 작성 */
				if (minute == 0 && second == 0)
				{
					Logger.LogReport(usdt, bnb, todayPnl, longBot.BaseOrderSize, longBot.Leverage, longBot.MaxActiveDeals);
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private async void Timer2s_Tick(object? sender, EventArgs e)
		{
			try
			{
				/* 자산, 포지션 모니터링 */
				(usdt, bnb) = await manager.GetBinanceAccountInfo().ConfigureAwait(false);
				await manager.GetBinanceOpenOrders().ConfigureAwait(false);
				DispatcherService.Invoke(() =>
				{
					PositionDataGrid.ItemsSource = Common.Positions;
					OrderDataGrid.ItemsSource = Common.OpenOrders;
					BlacklistDataGrid.ItemsSource = Common.BannedBlacklistPositions;
					//IndicatorDataGrid.ItemsSource = null;
					//IndicatorDataGrid.ItemsSource = Common.PairQuotes.OrderBy(x => x.Symbol);

					Common.Balance = usdt;
					BalanceText.Text = $"{usdt:#,###.###} USDT";
					SimpleBalanceText.Text = $"{usdt:#,###.###}";
					BnbText.Text = $"{bnb:#,###.###} BNB";
					SimpleBnbText.Text = $"{bnb:#,###.###}";

					/* 설정 저장 */
					Settings.Default.BaseOrderSize = BaseOrderSizeTextBox.Text;
					Settings.Default.Leverage = LeverageTextBox.Text;
					Settings.Default.MaxActiveDealsTypeIndex = MaxActiveDealsTypeComboBox.SelectedIndex;
					Settings.Default.MaxActiveDeals = MaxActiveDealsTextBox.Text;
					Settings.Default.Save();
				});
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		#region Theme Color
		private void ThemeColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (ThemeColorPicker.FindDescendant(typeof(Popup)) is Popup popup)
			{
				popup.IsOpen = true;
			}
		}

		private void ThemeColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			var _selectedColor = ThemeColorPicker.SelectedColor;
			if (_selectedColor == null)
			{
				return;
			}
			var selectedColor = _selectedColor.Value;

			Application.Current.Resources["ThemeColor"] = selectedColor;
			Application.Current.Resources["ThemeBrush"] = new SolidColorBrush(selectedColor);

			Settings.Default.ThemeColor = selectedColor.ToString();
			Settings.Default.Save();
		}

		private void ForegroundColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (ForegroundColorPicker.FindDescendant(typeof(Popup)) is Popup popup)
			{
				popup.IsOpen = true;
			}
		}

		private void ForegroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			var _selectedColor = ForegroundColorPicker.SelectedColor;
			if (_selectedColor == null)
			{
				return;
			}
			var selectedColor = _selectedColor.Value;

			Application.Current.Resources["ForegroundColor"] = selectedColor;
			Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(selectedColor);

			Settings.Default.ForegroundColor = selectedColor.ToString();
			Settings.Default.Save();
		}

		private void BackgroundColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (BackgroundColorPicker.FindDescendant(typeof(Popup)) is Popup popup)
			{
				popup.IsOpen = true;
			}
		}

		private void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			var _selectedColor = BackgroundColorPicker.SelectedColor;
			if (_selectedColor == null)
			{
				return;
			}
			var selectedColor = _selectedColor.Value;

			Application.Current.Resources["BackgroundColor"] = selectedColor;
			Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(selectedColor);

			Settings.Default.BackgroundColor = selectedColor.ToString();
			Settings.Default.Save();
		}

		private void LongColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (LongColorPicker.FindDescendant(typeof(Popup)) is Popup popup)
			{
				popup.IsOpen = true;
			}
		}

		private void LongColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			var _selectedColor = LongColorPicker.SelectedColor;
			if (_selectedColor == null)
			{
				return;
			}
			var selectedColor = _selectedColor.Value;

			Application.Current.Resources["LongColor"] = selectedColor;
			Application.Current.Resources["LongBrush"] = new SolidColorBrush(selectedColor);

			Settings.Default.LongColor = selectedColor.ToString();
			Settings.Default.Save();
		}

		private void ShortColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (ShortColorPicker.FindDescendant(typeof(Popup)) is Popup popup)
			{
				popup.IsOpen = true;
			}
		}

		private void ShortColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			var _selectedColor = ShortColorPicker.SelectedColor;
			if (_selectedColor == null)
			{
				return;
			}
			var selectedColor = _selectedColor.Value;

			Application.Current.Resources["ShortColor"] = selectedColor;
			Application.Current.Resources["ShortBrush"] = new SolidColorBrush(selectedColor);

			Settings.Default.ShortColor = selectedColor.ToString();
			Settings.Default.Save();
		}
		#endregion

		#region Full Screen
		private void FullScreen()
		{
			if (_isFullScreen)
			{
				WindowStyle = _previousWindowStyle;
				ResizeMode = _previousResizeMode;
				Top = _previousTop;
				Left = _previousLeft;
				Width = _previousWidth;
				Height = _previousHeight;
				WindowState = WindowState.Normal;
				_isFullScreen = false;
			}
			else
			{
				_previousWindowStyle = WindowStyle;
				_previousResizeMode = ResizeMode;
				_previousTop = Top;
				_previousLeft = Left;
				_previousWidth = Width;
				_previousHeight = Height;

				WindowStyle = WindowStyle.None;
				ResizeMode = ResizeMode.NoResize;
				Top = 0;
				Left = 0;
				Width = SystemParameters.PrimaryScreenWidth;
				Height = SystemParameters.PrimaryScreenHeight;
				WindowState = WindowState.Maximized;
				_isFullScreen = true;
			}
		}
		#endregion

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
				mockTimer.Tick += (s, e) =>
				{
					try
					{
						/* 포지션 모니터링 */
						DispatcherService.Invoke(() =>
						{
							PositionDataGrid.ItemsSource = null;
							PositionDataGrid.ItemsSource = mockBot.Positions;
							//IndicatorDataGrid.ItemsSource = null;
							//IndicatorDataGrid.ItemsSource = Common.PairQuotes.OrderBy(x => x.Symbol);
						});

						/* 자산 모니터링 */
						DispatcherService.Invoke(() =>
						{
							Common.Balance = mockBot.Money;
							BalanceText.Text = $"{mockBot.Money.Round(2)} USDT";
							//BnbText.Text = $"{bnb} BNB";
						});

						/* 수익 모니터링 */
						//DispatcherService.Invoke(() =>
						//{
						//	var incomes = mockBot.PositionHistory.Sum(x => x.Income);
						//	if (incomes >= 0)
						//	{
						//		TodayPnlText.Foreground = Common.LongColor;
						//		TodayPnlText.Text = $"+{incomes.Round(2)} USDT";
						//	}
						//	else
						//	{
						//		TodayPnlText.Foreground = Common.ShortColor;
						//		TodayPnlText.Text = $"{incomes.Round(2)} USDT";
						//	}
						//});

						/* 매시 0분 0초에 보고서 작성 */
						if (DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
						{
							Logger.LogReport(mockBot.Money, 0, todayPnl, mockBot.BaseOrderSize, mockBot.Leverage, mockBot.MaxActiveDeals);
						}

						if (mockBot.IsRunning)
						{
							mockBot.EvaluateLong();
							mockBot.EvaluateShort();
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
		private void LongBotCheckBox_Checked(object? sender, RoutedEventArgs e)
		{
			try
			{
				longBot.BaseOrderSize = int.Parse(BaseOrderSizeTextBox.Text);
				//longPosition.TargetRoe = decimal.Parse(TargetProfitTextBox.Text);
				longBot.Leverage = int.Parse(LeverageTextBox.Text);
				longBot.MaxActiveDeals = int.Parse(MaxActiveDealsTextBox.Text);
				longBot.MaxActiveDealsType = (MaxActiveDealsType)Enum.Parse(typeof(MaxActiveDealsType), (MaxActiveDealsTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Total");

				Common.AddHistory("Master", $"Long Bot On, BOS {longBot.BaseOrderSize}, LEV {longBot.Leverage}, MAD {longBot.MaxActiveDealsType},{longBot.MaxActiveDeals}");
				longBot.IsRunning = true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void LongBotCheckBox_Unchecked(object? sender, RoutedEventArgs e)
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

		private void ShortBotCheckBox_Checked(object? sender, RoutedEventArgs e)
		{
			try
			{
				shortBot.BaseOrderSize = int.Parse(BaseOrderSizeTextBox.Text);
				//shortPosition.TargetRoe = decimal.Parse(TargetProfitTextBox.Text);
				shortBot.Leverage = int.Parse(LeverageTextBox.Text);
				shortBot.MaxActiveDeals = int.Parse(MaxActiveDealsTextBox.Text);
				shortBot.MaxActiveDealsType = (MaxActiveDealsType)Enum.Parse(typeof(MaxActiveDealsType), (MaxActiveDealsTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Total");

				Common.AddHistory("Master", $"Short Bot On, BOS {shortBot.BaseOrderSize}, LEV {shortBot.Leverage}, MAD {shortBot.MaxActiveDealsType},{shortBot.MaxActiveDeals}");
				shortBot.IsRunning = true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void ShortBotCheckBox_Unchecked(object? sender, RoutedEventArgs e)
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

				return (int)(
					MaxActiveDealsTextBox.Text.ToInt()
					* BaseOrderSizeTextBox.Text.ToDecimal()
					/ LeverageTextBox.Text.ToInt()
					* ((MaxActiveDealsTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() == "Total" ? 1 : 2)
					);
			}
			catch
			{
				return 0;
			}
		}

		private void MaxActiveDealsTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (RequireAssetText == null)
			{
				return;
			}

			RequireAssetText.Text = $"{CalculateRequireAsset():N} USDT";
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

		#region Smart Seed
		private void SmartSeedCheckBox_Checked(object? sender, RoutedEventArgs e)
		{
			Common.AddHistory("Master", $"Smart Seed On");
		}

		private void SmartSeedCheckBox_Unchecked(object? sender, RoutedEventArgs e)
		{
			Common.AddHistory("Master", $"Smart Seed Off");
		}
		#endregion

		#region Indicator Grid
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
		private void IndicatorDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
			if (row != null && row.Item is PairQuote pairQuote)
			{
				var symbol = pairQuote.Symbol;
				var url = $"https://www.tradingview.com/chart/g2jIOGTD/?symbol=BINANCE%3A{symbol}.P";
				Start(url);
			}
		}
		#endregion

		/// <summary>
		/// 프로그램 종료
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Common.SaveBlacklist();

			if (longBot.IsRunning || shortBot.IsRunning)
			{
				if (MessageBox.Show("로봇이 작동 중입니다.\n정말 종료하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					Common.AddHistory("Program", "Program End");
					e.Cancel = false;
				}
				else
				{
					e.Cancel = true;
				}
			}
		}

		/// <summary>
		/// 프로그램 키 입력
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.F8: // 기본/심플 화면 전환
					if (MainGrid.Visibility == Visibility.Visible)
					{
						MainGrid.Visibility = Visibility.Hidden;
						SimpleGrid.Visibility = Visibility.Visible;
					}
					else
					{
						MainGrid.Visibility = Visibility.Visible;
						SimpleGrid.Visibility = Visibility.Hidden;
					}
					break;

				case Key.F9:
					var debugWindow = new DebugWindow()
					{
						LongBot = longBot,
						ShortBot = shortBot
					};
					debugWindow.Show();
					break;

				case Key.F11: // 창/전체 화면 전환
					FullScreen();
					break;

				case Key.F12:
					var reportWindow = new ReportWindow()
					{
						Bot = reportBot
					};
					reportWindow.Show();
					break;
			}
		}
	}
}
