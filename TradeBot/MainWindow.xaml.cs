using Mercury;
using Mercury.Enums;

using System;
using System.Collections.Generic;
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
		DispatcherTimer timer1m = new();
		ManagerBot manager = new("매니저 봇", "심볼 모니터링, 포지션 모니터링, 자산 모니터링 등등 전반적인 시스템을 관리하는 봇입니다.");
		LongBot longBot = new("롱 봇", "롱 포지션 매매를 하는 봇입니다.");
		ShortBot shortBot = new("숏 봇", "숏 포지션 매매를 하는 봇입니다.");
		MockBot mockBot = new("모의 봇", "모의 매매를 하는 봇입니다.");
		ReportBot reportBot = new("보고서 봇", "보고서 작성을 해주는 봇입니다.");

		IEnumerable<BinanceRealizedPnlHistory> todayRealizedPnlHistory = default!;

		decimal balance = -1;
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
				AdminText.Text = "Admin";
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

			Common.AddHistory = (subject, text) =>
			{
				DispatcherService.Invoke(() =>
				{
					var history = new BotHistory(DateTime.Now, subject, text);
					HistoryDataGrid.Items.Add(history);
					Logger.LogHistory(history);
				});
			};

			#region 통신 부분 (디버깅 시 주석 처리)
			var commResult = BinanceClients.Init();
			Common.AddHistory("API", commResult);

			await manager.GetAllKlines(5).ConfigureAwait(false);
			await manager.StartBinanceFuturesTicker().ConfigureAwait(false);

			timer.Interval = TimeSpan.FromMilliseconds(1000);
			timer.Tick += Timer_Tick;
			timer.Start();

			timer2s.Interval = TimeSpan.FromSeconds(2);
			timer2s.Tick += Timer2s_Tick;
			timer2s.Start();

			#endregion
		}

		private void Timer1m_Tick(object? sender, EventArgs e)
		{
			try
			{
				// 소리 재생 안하면 스킵
				//if (!Common.IsSound)
				//{
				//	return;
				//}

				//// 자산이 상한선을 넘으면 소리 재생
				//if (decimal.TryParse(UpperAlarmTextBox.Text, out var upper))
				//{
				//	if (balance > upper)
				//	{
				//		Sound.Play("Resources/upper.wav", 0.5);
				//	}
				//}

				//// 자산이 하한선을 넘으면 소리 재생
				//if (decimal.TryParse(LowerAlarmTextBox.Text, out var lower))
				//{
				//	if (balance < lower)
				//	{
				//		Sound.Play("Resources/lower.wav", 0.5);
				//	}
				//}

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

				/* 매시 0분 0초에 보고서 작성 */
				if (minute == 0 && second == 0)
				{
					Logger.LogReport(usdt, bnb, todayPnl, longBot.BaseOrderSize, longBot.Leverage, longBot.MaxActiveDeals);
				}

				/* 매시 15분 5초에 모든 미체결주문 취소 */
				if (minute == 15 && second == 0)
				{
					foreach (var openOrder in Common.OpenOrders)
					{
						await BinanceClients.Api.UsdFuturesApi.Trading.CancelAllOrdersAfterTimeoutAsync(openOrder.Symbol, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
						Common.AddHistory("Master", $"Cancel Order {openOrder.Symbol}, {openOrder.Side}, {openOrder.FilledString}");
					}
				}

				/* 매시 0분 0초부터 0분 5초까지는 포지션 정리 */
				if (minute * 60 + second <= 5)
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
				/* 매시 0분 6초부터 2분 0초까지는 포지션 진입 */
				else if (minute * 60 + second >= 6 && minute * 60 + second <= 120)
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
					//IndicatorDataGrid.ItemsSource = null;
					//IndicatorDataGrid.ItemsSource = Common.PairQuotes.OrderBy(x => x.Symbol);

					balance = usdt;
					BalanceText.Text = $"{usdt:#,###.###} USDT";
					BnbText.Text = $"{bnb:#,###.####} BNB";

					/* 설정 저장 */
					Settings.Default.BaseOrderSize = BaseOrderSizeTextBox.Text;
					Settings.Default.Leverage = LeverageTextBox.Text;
					Settings.Default.MaxActiveDealsTypeIndex = MaxActiveDealsTypeComboBox.SelectedIndex;
					Settings.Default.MaxActiveDeals = MaxActiveDealsTextBox.Text;
					Settings.Default.Save();
				});

				/* 수익 모니터링 */
				//todayRealizedPnlHistory = await manager.GetBinanceTodayRealizedPnlHistory();
				//DispatcherService.Invoke(() =>
				//{
				//	if (todayRealizedPnlHistory != null && !todayRealizedPnlHistory.Any(x => x == null))
				//	{
				//		todayPnl = todayRealizedPnlHistory.Sum(x => x.RealizedPnl);
				//		if (todayPnl >= 0)
				//		{
				//			TodayPnlText.Foreground = Common.LongColor;
				//			TodayPnlText.Text = $"+{todayPnl:#,###.###} USDT";
				//		}
				//		else
				//		{
				//			TodayPnlText.Foreground = Common.ShortColor;
				//			TodayPnlText.Text = $"{todayPnl:#,###.###} USDT";
				//		}
				//	}


				//});
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

		#region Collect Info
		//private async void CollectTradeButton_Click(object sender, RoutedEventArgs e)
		//{
		//	try
		//	{
		//		await reportBot.WriteTradeReport().ConfigureAwait(false);
		//		Common.AddHistory("Master", "Collect Trade Complete");
		//	}
		//	catch (Exception ex)
		//	{
		//		Common.AddHistory("Master", ex.Message);
		//		Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
		//	}
		//}

		//private async void CollectIncomeButton_Click(object sender, RoutedEventArgs e)
		//{
		//	try
		//	{
		//		await reportBot.WriteIncomeReport().ConfigureAwait(false);
		//		Common.AddHistory("Master", "Collect Income Complete");
		//	}
		//	catch (Exception ex)
		//	{
		//		Common.AddHistory("Master", ex.Message);
		//		Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
		//	}
		//}

		//private void CollectDailyButton_Click(object sender, RoutedEventArgs e)
		//{
		//	try
		//	{
		//		reportBot.WriteDailyReport();
		//		Common.AddHistory("Master", "Collect Daily Complete");
		//	}
		//	catch (Exception ex)
		//	{
		//		Common.AddHistory("Master", ex.Message);
		//		Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
		//	}
		//}
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
							balance = mockBot.Money;
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
		private void LongBotCheckBox_Checked(object sender, RoutedEventArgs e)
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
				shortBot.MaxActiveDealsType = (MaxActiveDealsType)Enum.Parse(typeof(MaxActiveDealsType), (MaxActiveDealsTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Total");

				Common.AddHistory("Master", $"Short Bot On, BOS {shortBot.BaseOrderSize}, LEV {shortBot.Leverage}, MAD {shortBot.MaxActiveDealsType},{shortBot.MaxActiveDeals}");
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


		//private void TodayPnlText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//	try
		//	{
		//		var pnlWindow = new RealizedPnlWindow();
		//		pnlWindow.Init(todayRealizedPnlHistory);
		//		pnlWindow.Show();
		//	}
		//	catch (Exception ex)
		//	{
		//		Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex);
		//	}
		//}


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

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.F11:
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
