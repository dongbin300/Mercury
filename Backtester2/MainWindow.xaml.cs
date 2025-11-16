using Backtester2.Apis;
using Backtester2.Models;

using Binance.Net.Enums;

using Mercury;
using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

using Microsoft.Win32;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Backtester2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// RLCUSDT;UNFIUSDT;LPTUSDT;QTUMUSDT;OMGUSDT;CHZUSDT;STORJUSDT;KNCUSDT;BALUSDT;COMPUSDT;GALUSDT;YFIUSDT;MTLUSDT;IMXUSDT;ENSUSDT;DASHUSDT;MANAUSDT;WAVESUSDT;MATICUSDT;BLZUSDT;ZENUSDT;SFPUSDT;SANDUSDT;BCHUSDT;LTCUSDT;TRXUSDT;ALPHAUSDT;ETCUSDT;ENJUSDT;ARUSDT
	/// 
	/// 
	/// BTCUSDT;ETHUSDT;BCHUSDT;XRPUSDT;LTCUSDT;TRXUSDT;ETCUSDT;XLMUSDT;ADAUSDT;XMRUSDT;BNBUSDT;VETUSDT;NEOUSDT;THETAUSDT;DOGEUSDT;BANDUSDT;RLCUSDT;YFIUSDT;TRBUSDT;SUSHIUSDT;EGLDUSDT;SOLUSDT;UNIUSDT;AVAXUSDT;ENJUSDT;KSMUSDT;AAVEUSDT;RSRUSDT;LRCUSDT;ZENUSDT;GRTUSDT;SANDUSDT;HBARUSDT;MTLUSDT;BTCDOMUSDT;MASKUSDT;ARUSDT;LPTUSDT;ENSUSDT;DUSKUSDT;IMXUSDT;API3USDT;APEUSDT;WOOUSDT;JASMYUSDT;OPUSDT;INJUSDT;LDOUSDT;APTUSDT;QNTUSDT;FETUSDT;FXSUSDT;HIGHUSDT;ASTRUSDT;PHBUSDT;GMXUSDT;CFXUSDT;STXUSDT;ACHUSDT;SSVUSDT;LQTYUSDT;USDCUSDT;IDUSDT;JOEUSDT;HFTUSDT;XVSUSDT;BLURUSDT;SUIUSDT;NMRUSDT;MAVUSDT;XVGUSDT;WLDUSDT;PENDLEUSDT;ARKMUSDT;AGLDUSDT;DODOXUSDT;BNTUSDT;OXTUSDT;BIGTIMEUSDT;RIFUSDT;POLYXUSDT;TIAUSDT;CAKEUSDT;TWTUSDT;ORDIUSDT;STEEMUSDT;ILVUSDT;KASUSDT;BEAMXUSDT;PYTHUSDT;SUPERUSDT;USTCUSDT;ONGUSDT;ETHWUSDT;JTOUSDT;AUCTIONUSDT;ACEUSDT;MOVRUSDT;NFPUSDT
	/// 
	/// 2020-01-01 ~
	/// BTCUSDT;ETHUSDT;BCHUSDT;XRPUSDT;LTCUSDT;TRXUSDT;ETCUSDT;XLMUSDT;ADAUSDT;XMRUSDT;BNBUSDT;VETUSDT;NEOUSDT;THETAUSDT;DOGEUSDT;BANDUSDT;RLCUSDT;YFIUSDT;TRBUSDT;SUSHIUSDT;EGLDUSDT;SOLUSDT;UNIUSDT;AVAXUSDT;ENJUSDT;KSMUSDT;AAVEUSDT;RSRUSDT;LRCUSDT;ZENUSDT
	/// </summary>
	public partial class MainWindow : Window
	{
		// Helper class for progress updates
		public class BacktestProgressUpdate
		{
			public List<string>? Header { get; set; }
			public required string CsvLine { get; set; }
		}

		// Data classes for run info
		public class RunInfo
		{
			public string Symbol { get; set; } = string.Empty;
			public string Strategy { get; set; } = string.Empty;
			public string Intervals { get; set; } = string.Empty;
			public string Period { get; set; } = string.Empty;
			public string MaxPositions { get; set; } = string.Empty;
			public string Leverage { get; set; } = string.Empty;
			public string RunTime { get; set; } = string.Empty;
		}

		// Data class for parameter statistics
		public class ParameterValueStats
		{
			public string Parameter { get; set; } = string.Empty;
			public string Value { get; set; } = string.Empty;
			public string WinRate { get; set; } = string.Empty;
			public string Money { get; set; } = string.Empty;
			public string Mdd { get; set; } = string.Empty;
			public string Risk { get; set; } = string.Empty;
			public double WinRateBarWidth { get; set; }
			public double MoneyBarWidth { get; set; }
			public double MddBarWidth { get; set; }
			public double RiskBarWidth { get; set; }
		}

		// Data class for backtest results with expandable details
		public class BacktestResultRow
		{
			public Dictionary<string, object> MainData { get; set; } = new();
			public string ReportFileName { get; set; } = string.Empty;
			public bool IsExpanded { get; set; } = false;
			public List<DailyData>? DailyDetails { get; set; }
		}

		public class DailyData
		{
			public string DateTime { get; set; } = string.Empty;
			public string Win { get; set; } = string.Empty;
			public string Lose { get; set; } = string.Empty;
			public string WinRate { get; set; } = string.Empty;
			public string LongPositionCount { get; set; } = string.Empty;
			public string ShortPositionCount { get; set; } = string.Empty;
			public string EstimatedMoney { get; set; } = string.Empty;
			public string Change { get; set; } = string.Empty;
			public string ChangePer { get; set; } = string.Empty;
			public string MaxPer { get; set; } = string.Empty;
		}


		private BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

		// Multi-page data management
		private List<List<Dictionary<string, object>>> backtestPages = new();
		private List<List<string>> pageHeaders = new(); // For storing headers of each page
		private List<RunInfo> pageRunInfos = new(); // For storing run info of each page
		private List<string> currentHeader = new(); // For the currently running backtest
		private RunInfo? currentRunInfo = null; // For the currently running backtest
		private int currentPageIndex = 0;
		private DataTable currentPageData = new();

		// File monitoring
		private FileSystemWatcher? fileWatcher;
		private string? monitoredFilePath;

		private int selectedIndex = -1;

		// Symbol data class for DataGrid
		public class SymbolInfo
		{
			public string Symbol { get; set; } = string.Empty;
			public string StartDate { get; set; } = string.Empty;
			public string EndDate { get; set; } = string.Empty;
		}
		private Assembly assembly = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mercury.dll"));
		private readonly string StrategyNamespace = "Mercury.Backtests.BacktestStrategies";
		private readonly string ReportDirectory = MercuryPath.Desktop;

		private SmartRandom random = new();

		// Position history progressive loading
		private string? currentPositionFile = null;
		private string[]? allPositionLines = null;
		private string[]? positionHeaders = null;
		private int positionHeaderIndex = -1;
		private int currentLoadedPositions = 0;
		private bool isLoadingMorePositions = false;
		private ObservableCollection<Dictionary<string, string>>? positionDataCollection = null;

		private readonly string StrategyParameterValueFile = Path.Combine(MercuryPath.Base, "bt_spv.json");
		private AddOrUpdateDictionary<string, string> spvData = [];

		public MainWindow()
		{
			InitializeComponent();

			// Add cleanup on window closing
			Closing += (s, e) => StopFileMonitoring();

			LocalStorageApi.Init();

			// Initialize multi-page data structures
			InitializeDataStructures();

			var symbols = Settings.Default.Symbol.Split(';') ?? [];
			foreach (var symbol in symbols)
			{
				if (!string.IsNullOrEmpty(symbol.Trim()))
				{
					AddSymbolTicker(symbol.Trim());
				}
			}
			StartDateTextBox.Text = Settings.Default.StartDate.Trim();
			EndDateTextBox.Text = Settings.Default.EndDate.Trim();
			SeedTextBox.Text = Settings.Default.Seed.Trim();
			LeverageTextBox.Text = Settings.Default.Leverage.Trim();
			MaxPositionsTextBox.Text = Settings.Default.MaxPositions.Trim();
			MaxPositionsEachCheckBox.IsChecked = Settings.Default.MaxPositionsEach;
			FileNameTextBox.Text = Settings.Default.FileName.Trim();
			StrategyComboBox.SelectedItem = Settings.Default.Strategy.Trim();
			Interval1ComboBox.SelectedItem = Settings.Default.Interval1.Trim();
			Interval2ComboBox.SelectedItem = Settings.Default.Interval2.Trim();
			Interval3ComboBox.SelectedItem = Settings.Default.Interval3.Trim();
			FeeRateTextBox.Text = Settings.Default.FeeRate.Trim();
			ReportDailyHistoryCheckBox.IsChecked = Settings.Default.DailyHistory;
			ReportPositionHistoryCheckBox.IsChecked = Settings.Default.PositionHistory;
			ReportTradeHistoryCheckBox.IsChecked = Settings.Default.TradeHistory;
			SingleSymbolCheckBox.IsChecked = Settings.Default.SingleSymbol;
			RandomSymbolCheckBox.IsChecked = Settings.Default.RandomSymbol;
			RandomSymbolCountTextBox.Text = Settings.Default.RandomSymbolCount.Trim();
			RandomDateCheckBox.IsChecked = Settings.Default.RandomDate;
			RandomDatePeriodTextBox.Text = Settings.Default.RandomDatePeriod.Trim();
			LongEnableCheckBox.IsChecked = Settings.Default.LongEnable;
			ShortEnableCheckBox.IsChecked = Settings.Default.ShortEnable;

			var spvJsonString = File.ReadAllText(StrategyParameterValueFile);
			spvData = JsonConvert.DeserializeObject<AddOrUpdateDictionary<string, string>>(spvJsonString) ?? [];

			var strategyNames = assembly.GetTypes().Where(t => t.IsClass && t.IsPublic && !t.Name.Contains('`') && !t.Name.Contains('<') && t.Namespace == StrategyNamespace).Select(t => t.Name).ToList();
			StrategyComboBox.ItemsSource = strategyNames;

			var intervals = new List<string>() { "None", "1m", "5m", "15m", "30m", "1h", "2h", "4h", "1D" };
			Interval1ComboBox.ItemsSource = intervals;
			Interval2ComboBox.ItemsSource = intervals;
			Interval3ComboBox.ItemsSource = intervals;

			// 기본 파일이 있으면 자동 로드
			if (!string.IsNullOrEmpty(FileNameTextBox.Text))
			{
				var defaultFilePath = Path.Combine(ReportDirectory, $"{FileNameTextBox.Text}.csv");
				if (File.Exists(defaultFilePath))
				{
					LoadResultsFromFile(defaultFilePath);
				}
			}

			//WindowState = WindowState.Minimized;
			//RunButton_Click(null, null);

		}

		private void InitializeDataStructures()
		{
			backtestPages.Clear();
			pageHeaders.Clear();
			pageRunInfos.Clear();
			currentHeader.Clear();
			currentRunInfo = null;
			currentPageIndex = 0;
			currentPageData = new DataTable();

			BacktestResultsDataGrid.ItemsSource = currentPageData.DefaultView;
			RunInfoPanel.Visibility = Visibility.Collapsed;
			UpdatePageNavigation();
		}

		private void SaveSettings()
		{
			Settings.Default.Symbol = string.Join(";", GetSymbols());
			Settings.Default.StartDate = StartDateTextBox.Text.Trim();
			Settings.Default.EndDate = EndDateTextBox.Text.Trim();
			Settings.Default.Seed = SeedTextBox.Text.Replace(",", "").Trim();
			Settings.Default.Leverage = LeverageTextBox.Text.Trim();
			Settings.Default.MaxPositions = MaxPositionsTextBox.Text.Trim();
			Settings.Default.MaxPositionsEach = MaxPositionsEachCheckBox.IsChecked == true;
			Settings.Default.FileName = FileNameTextBox.Text.Trim();
			Settings.Default.Strategy = StrategyComboBox.SelectedItem.ToString() ?? string.Empty;
			Settings.Default.Interval1 = Interval1ComboBox.SelectedItem.ToString() ?? string.Empty;
			Settings.Default.Interval2 = Interval2ComboBox.SelectedItem.ToString() ?? string.Empty;
			Settings.Default.Interval3 = Interval3ComboBox.SelectedItem.ToString() ?? string.Empty;
			Settings.Default.FeeRate = FeeRateTextBox.Text.Trim();
			Settings.Default.DailyHistory = ReportDailyHistoryCheckBox.IsChecked == true;
			Settings.Default.PositionHistory = ReportPositionHistoryCheckBox.IsChecked == true;
			Settings.Default.TradeHistory = ReportTradeHistoryCheckBox.IsChecked == true;
			Settings.Default.SingleSymbol = SingleSymbolCheckBox.IsChecked == true;
			Settings.Default.RandomSymbol = RandomSymbolCheckBox.IsChecked == true;
			Settings.Default.RandomSymbolCount = RandomSymbolCountTextBox.Text.Trim();
			Settings.Default.RandomDate = RandomDateCheckBox.IsChecked == true;
			Settings.Default.RandomDatePeriod = RandomDatePeriodTextBox.Text.Trim();
			Settings.Default.LongEnable = LongEnableCheckBox.IsChecked == true;
			Settings.Default.ShortEnable = ShortEnableCheckBox.IsChecked == true;
			Settings.Default.Save();

			strategyParameters.Clear();
			foreach (var tb in StrategyParameterPanel.Children.OfType<TextBox>())
			{
				string key = tb.Tag?.ToString() ?? tb.Name.Replace("SP_", "");
				string value = tb.Text.Trim();
				strategyParameters.TryAdd(key, value);
				spvData.Update($"{StrategyComboBox.SelectedItem}.{key}", value);
			}

			var spvJsonString = JsonConvert.SerializeObject(spvData, Formatting.Indented);
			File.WriteAllText(StrategyParameterValueFile, spvJsonString);
		}

		#region Symbol
		private List<string> GetSymbols()
		{
			return [.. SymbolTickerPanel.Children.OfType<Border>().Select(b => b.Child).OfType<TextBlock>().Select(t => t.Text)];
		}

		private void SymbolInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = SymbolInputTextBox.Text.Trim();

			if (string.IsNullOrEmpty(text))
			{
				SymbolFilteredDataGrid.Visibility = Visibility.Collapsed;
				SymbolFilteredDataGrid.ItemsSource = null;
				selectedIndex = -1;
				return;
			}

			var filtered = LocalStorageApi.Symbols.Where(i => i.Item1.ToLower().Contains(text, StringComparison.CurrentCultureIgnoreCase))
				.Select(item => new SymbolInfo
				{
					Symbol = item.Item1,
					StartDate = item.Item2.ToString("yyyy-MM-dd"),
					EndDate = item.Item3.ToString("yyyy-MM-dd")
				}).ToList();

			SymbolFilteredDataGrid.ItemsSource = filtered;

			if (filtered.Count > 0)
			{
				SymbolFilteredDataGrid.Visibility = Visibility.Visible;
				selectedIndex = 0;
				SymbolFilteredDataGrid.SelectedIndex = selectedIndex;
			}
			else
			{
				SymbolFilteredDataGrid.Visibility = Visibility.Collapsed;
				selectedIndex = -1;
			}
		}

		private void SymbolInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (SymbolInputTextBox.Text.Contains(';'))
			{
				var symbols = SymbolInputTextBox.Text.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

				foreach (var symbol in symbols)
				{
					if (GetSymbols().Any(x => x.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
					{
						continue;
					}
					AddSymbolTicker(symbol);
				}
				SymbolInputTextBox.Clear();
			}

			if (SymbolFilteredDataGrid.Visibility != Visibility.Visible || SymbolFilteredDataGrid.Items.Count == 0)
				return;

			if (e.Key == Key.Down)
			{
				selectedIndex++;
				if (selectedIndex >= SymbolFilteredDataGrid.Items.Count)
					selectedIndex = 0;

				SymbolFilteredDataGrid.SelectedIndex = selectedIndex;
				SymbolFilteredDataGrid.ScrollIntoView(SymbolFilteredDataGrid.SelectedItem);
				e.Handled = true;
			}
			else if (e.Key == Key.Up)
			{
				selectedIndex--;
				if (selectedIndex < 0)
					selectedIndex = SymbolFilteredDataGrid.Items.Count - 1;

				SymbolFilteredDataGrid.SelectedIndex = selectedIndex;
				SymbolFilteredDataGrid.ScrollIntoView(SymbolFilteredDataGrid.SelectedItem);
				e.Handled = true;
			}
			else if (e.Key == Key.Enter)
			{
				if (selectedIndex >= 0 && selectedIndex < SymbolFilteredDataGrid.Items.Count && SymbolFilteredDataGrid.SelectedItem is SymbolInfo selectedSymbolInfo)
				{
					var selectedSymbol = selectedSymbolInfo.Symbol;

					var currentSymbols = GetSymbols();
					if (currentSymbols.Any(x => x.Equals(selectedSymbol)))
					{
						MessageBox.Show("이미 존재하는 심볼입니다.");
						return;
					}

					AddSymbolTicker(selectedSymbol);

					SymbolInputTextBox.Clear();
					SymbolFilteredDataGrid.ItemsSource = null;
					SymbolFilteredDataGrid.Visibility = Visibility.Collapsed;
					selectedIndex = -1;

					SaveSettings();
				}
				e.Handled = true;
			}
		}

		private void AddSymbolTicker(string symbol)
		{
			var tickerBorder = new Border
			{
				Background = Brushes.Transparent,
				BorderBrush = Brushes.White,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(3),
				Margin = new Thickness(2),
				Padding = new Thickness(2),
			};
			var tickerText = new TextBlock
			{
				Text = symbol,
				FontSize = 10.5,
			};
			tickerBorder.Child = tickerText;
			tickerBorder.MouseLeftButtonUp += (s, args) =>
			{
				SymbolTickerPanel.Children.Remove(tickerBorder);
				UpdateSymbolCount();
			};
			SymbolTickerPanel.Children.Add(tickerBorder);
			UpdateSymbolCount();
		}

		private void UpdateSymbolCount()
		{
			int count = SymbolTickerPanel.Children.Count;
			SymbolCountTextBlock.Text = $"{count}";
		}

		private void RandomSymbolCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			RandomSymbolCountTextBox.Focus();
		}

		private void SymbolFilteredDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SymbolFilteredDataGrid.SelectedItem is SymbolInfo selectedSymbolInfo)
			{
				var selectedSymbol = selectedSymbolInfo.Symbol;

				var currentSymbols = GetSymbols();
				if (currentSymbols.Any(x => x.Equals(selectedSymbol)))
				{
					MessageBox.Show("이미 존재하는 심볼입니다.");
					return;
				}

				AddSymbolTicker(selectedSymbol);

				SymbolInputTextBox.Clear();
				SymbolFilteredDataGrid.ItemsSource = null;
				SymbolFilteredDataGrid.Visibility = Visibility.Collapsed;
				selectedIndex = -1;

				SaveSettings();
			}
		}
		#endregion

		#region Date
		private void StartDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = StartDateTextBox.Text.Trim();
			if (text.Length == 6)
			{
				StartDateTextBox.Text = $"20{text[..2]}-{text[2..4]}-{text[4..]}";
				EndDateTextBox.Focus();
			}
		}

		private void EndDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = EndDateTextBox.Text.Trim();
			if (text.Length == 6)
			{
				EndDateTextBox.Text = $"20{text[..2]}-{text[2..4]}-{text[4..]}";
			}
		}

		private void RandomDateCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			RandomDatePeriodTextBox.Focus();
		}
		#endregion

		#region Strategy
		private FieldInfo[] GetFields(Type type)
		{
			return type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
		}

		private void StrategyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedStrategy = StrategyComboBox.SelectedItem.ToString();
			if (string.IsNullOrEmpty(selectedStrategy))
			{
				return;
			}

			var targetType = assembly.GetTypes().FirstOrDefault(t => t.IsClass && t.IsPublic && t.Namespace == StrategyNamespace && t.Name == selectedStrategy);
			if (targetType == null)
			{
				return;
			}

			var instance = Activator.CreateInstance(targetType, "", 0m, 0, MaxActiveDealsType.Total, 0);
			var fields = GetFields(targetType);

			StrategyParameterPanel.Children.Clear();

			foreach (var field in fields)
			{
				var fieldValue = field.GetValue(instance);
				var textblock = new TextBlock
				{
					Text = field.Name,
					Style = (Style)FindResource("Description"),
					Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
					Margin = new Thickness(0, 10, 0, 3)
				};
				var textbox = new TextBox
				{
					Name = "SP_" + field.Name,
					Text = fieldValue?.ToString() ?? string.Empty,
					Tag = field.Name,
					Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
					Foreground = Brushes.White,
					BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
					BorderThickness = new Thickness(1, 1, 1, 1),
					Padding = new Thickness(5, 2, 5, 2)
				};

				// 저장된 파라미터 값이 있다면 불러오기
				if (spvData.TryGetValue($"{selectedStrategy}.{field.Name}", out string? savedValue))
				{
					textbox.Text = savedValue;
				}

				textbox.TextChanged += StrategyParameterTextbox_TextChanged;

				StrategyParameterPanel.Children.Add(textblock);
				StrategyParameterPanel.Children.Add(textbox);
			}

			var processCountTextBlock = new TextBlock
			{
				Name = "ProcessCountTextBlock",
				Margin = new Thickness(5, 0, 0, 10),
				FontWeight = FontWeights.Bold,
				Text = "0",
				Foreground = (Brush)Application.Current.Resources["Long2"],
			};
			StrategyParameterPanel.Children.Add(processCountTextBlock);

			StrategyParameterTextbox_TextChanged(sender, default!);
		}

		private void StrategyParameterTextbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var selectedStrategy = StrategyComboBox.SelectedItem?.ToString();
			if (string.IsNullOrEmpty(selectedStrategy))
				return;

			var targetType = assembly.GetTypes().FirstOrDefault(t => t.IsClass && t.IsPublic && t.Namespace == StrategyNamespace && t.Name == selectedStrategy);
			if (targetType == null)
				return;

			var fields = GetFields(targetType);
			decimal totalCount = 1;

			foreach (var field in fields)
			{
				var textbox = StrategyParameterPanel.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Tag?.ToString() == field.Name);

				if (textbox == null)
					continue;

				string text = textbox.Text;
				int count = 1;

				if (!string.IsNullOrEmpty(text) && text.Contains(';'))
				{
					var parts = text.Split(';');
					if (parts.Length == 3
						&& decimal.TryParse(parts[0], out decimal start)
						&& decimal.TryParse(parts[1], out decimal step)
						&& decimal.TryParse(parts[2], out decimal end)
						&& step != 0)
					{
						count = (int)((end - start) / step) + 1;
						if (count < 1) count = 1;
					}
				}

				totalCount *= count;
			}

			var processCountBlock = StrategyParameterPanel.Children.OfType<TextBlock>().FirstOrDefault(c => c is TextBlock tb && tb.Name == "ProcessCountTextBlock");

			processCountBlock?.Text = totalCount.ToString();

			if (totalCount > 1)
			{
				ReportDailyHistoryCheckBox.IsEnabled = false;
				ReportPositionHistoryCheckBox.IsEnabled = false;
				ReportTradeHistoryCheckBox.IsEnabled = false;
			}
			else
			{
				ReportDailyHistoryCheckBox.IsEnabled = true;
				ReportPositionHistoryCheckBox.IsEnabled = true;
				ReportTradeHistoryCheckBox.IsEnabled = true;
			}
		}

		#endregion

		#region Seed
		private bool _isSeedEditing = false;
		private void SeedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !NumericRegex().IsMatch(e.Text);
		}

		private void SeedTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isSeedEditing)
			{
				return;
			}

			_isSeedEditing = true;

			if (sender is not TextBox textBox)
			{
				return;
			}

			string digitsOnly = NonDigitRegex().Replace(textBox.Text, "");

			if (ulong.TryParse(digitsOnly, out ulong number))
			{
				textBox.Text = number.ToString("N0");
				textBox.SelectionStart = textBox.Text.Length;
			}
			else
			{
				textBox.Text = "";
			}

			_isSeedEditing = false;
		}
		#endregion

		List<string> symbols = [];
		string? interval1String = string.Empty;
		string? interval2String = string.Empty;
		string? interval3String = string.Empty;
		DateTime startDate = DateTime.MinValue;
		DateTime endDate = DateTime.MaxValue;
		string reportFileName = string.Empty;
		string strategy = string.Empty;
		MaxActiveDealsType maxPositionsType = MaxActiveDealsType.Total;
		int maxPositions = 0;
		decimal seed = 0m;
		int leverage = 0;
		decimal feeRate = 0m;
		bool isGenerateDailyHistory = false;
		bool isGeneratePositionHistory = false;
		bool isGenerateTradeHistory = false;
		bool isSingleSymbol = false;
		bool isRandomSymbol = false;
		int randomSymbolCount = 0;
		bool isRandomDate = false;
		int randomDatePeriod = 0;
		bool isLongEnable = false;
		bool isShortEnable = false;
		Dictionary<string, string> strategyParameters = [];

		private void RunButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SaveSettings();

				// 백테스트 시작 전 결과 테이블 초기화
				InitializeDataStructures();
				StatusTextBlock.Text = "Starting backtest...";

				// Initialize progress bar
				BacktestProgressBar.Value = 0;
				BacktestProgressBar.Visibility = Visibility.Visible;

				currentHeader.Clear();
				symbols = GetSymbols();
				interval1String = Interval1ComboBox.SelectedItem.ToString();
				interval2String = Interval2ComboBox.SelectedItem.ToString();
				interval3String = Interval3ComboBox.SelectedItem.ToString();
				startDate = StartDateTextBox.Text.ToDateTime();
				endDate = EndDateTextBox.Text.ToDateTime();
				reportFileName = FileNameTextBox.Text.Trim();
				strategy = StrategyComboBox.SelectedItem.ToString() ?? string.Empty;
				maxPositionsType = MaxPositionsEachCheckBox.IsChecked == true ? MaxActiveDealsType.Each : MaxActiveDealsType.Total;
				maxPositions = MaxPositionsTextBox.Text.ToInt();
				seed = SeedTextBox.Text.ToDecimal();
				leverage = LeverageTextBox.Text.ToInt();
				feeRate = FeeRateTextBox.Text.ToDecimal() / 100m;
				isGenerateDailyHistory = ReportDailyHistoryCheckBox.IsEnabled == true && ReportDailyHistoryCheckBox.IsChecked == true;
				isGeneratePositionHistory = ReportPositionHistoryCheckBox.IsEnabled == true && ReportPositionHistoryCheckBox.IsChecked == true;
				isGenerateTradeHistory = ReportTradeHistoryCheckBox.IsEnabled == true && ReportTradeHistoryCheckBox.IsChecked == true;
				isSingleSymbol = SingleSymbolCheckBox.IsChecked == true;
				isRandomSymbol = RandomSymbolCheckBox.IsChecked == true;
				randomSymbolCount = RandomSymbolCountTextBox.Text.ToInt();
				isRandomDate = RandomDateCheckBox.IsChecked == true;
				randomDatePeriod = RandomDatePeriodTextBox.Text.ToInt();
				isLongEnable = LongEnableCheckBox.IsChecked == true;
				isShortEnable = ShortEnableCheckBox.IsChecked == true;

				worker.DoWork += Worker_DoWork;
				worker.ProgressChanged += Worker_ProgressChanged;
				worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
				worker.RunWorkerAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		#region Util
		private static object GetDefault(Type t)
		{
			if (t.IsValueType)
				return Activator.CreateInstance(t)!;
			return null!;
		}

		private static bool TryParse(string? text, Type targetType, out dynamic? value)
		{
			value = null;
			if (string.IsNullOrEmpty(text))
				return false;

			try
			{
				if (targetType == typeof(int))
					value = int.Parse(text);
				else if (targetType == typeof(decimal))
					value = decimal.Parse(text);
				else if (targetType == typeof(double))
					value = double.Parse(text);
				else if (targetType == typeof(float))
					value = float.Parse(text);
				else if (targetType == typeof(long))
					value = long.Parse(text);
				else
					value = Convert.ChangeType(text, targetType);

				return true;
			}
			catch
			{
				return false;
			}
		}

		private static int Compare(dynamic a, dynamic b, Type type)
		{
			if (type == typeof(int))
				return ((int)a).CompareTo((int)b);
			if (type == typeof(decimal))
				return ((decimal)a).CompareTo((decimal)b);
			if (type == typeof(double))
				return ((double)a).CompareTo((double)b);
			if (type == typeof(float))
				return ((float)a).CompareTo((float)b);
			if (type == typeof(long))
				return ((long)a).CompareTo((long)b);
			return 0;
		}
		#endregion

		private void Worker_DoWork(object? sender, DoWorkEventArgs e)
		{
			try
			{
				int count = 0;
				List<string> selectedSymbols = symbols;
				DateTime selectedStartDate = startDate;
				DateTime selectedEndDate = endDate;
				List<List<ChartPack>> chartPacks = [];
				List<ChartPack> chartPacks1 = []; // 메인 인터벌
				List<ChartPack> chartPacks2 = []; // 서브1 인터벌
				List<ChartPack> chartPacks3 = []; // 서브2 인터벌

				// 심볼 선택
				selectedSymbols =
					isSingleSymbol
						? (isRandomSymbol ? new List<string> { random.Next(symbols) } : [symbols[count]])
						: (isRandomSymbol ? random.Next(symbols, randomSymbolCount) : symbols);

				// 백테스트 기간 선택
				if (isRandomDate)
				{
					selectedStartDate = random.Next(startDate, endDate.AddDays(-randomDatePeriod));
					selectedEndDate = selectedStartDate.AddDays(randomDatePeriod);
				}
				else
				{
					selectedStartDate = startDate;
					selectedEndDate = endDate;
				}

				// 차트팩 로드 함수
				void LoadChartPacks(string? intervalString, List<ChartPack> packs)
				{
					if (!string.IsNullOrEmpty(intervalString) && intervalString != "None")
					{
						foreach (var symbol in selectedSymbols)
						{
							var chartPack = ChartLoader.InitCharts(symbol, intervalString.ToKlineInterval(), selectedStartDate, selectedEndDate);
							packs.Add(chartPack);
						}
					}
				}

				LoadChartPacks(interval1String, chartPacks1);
				LoadChartPacks(interval2String, chartPacks2);
				LoadChartPacks(interval3String, chartPacks3);
				chartPacks.Add(chartPacks1);
				chartPacks.Add(chartPacks2);
				chartPacks.Add(chartPacks3);

				// 시작 로그 기록 및 현재 실행 정보 저장
				currentRunInfo = new RunInfo
				{
					Symbol = selectedSymbols.Count > 1 ? $"{selectedSymbols[0]} (+{selectedSymbols.Count - 1})" : selectedSymbols[0],
					Strategy = strategy,
					Intervals = string.Join(", ", new[] { interval1String, interval2String, interval3String }.Where(i => !string.IsNullOrEmpty(i) && i != "None")),
					Period = $"{selectedStartDate:yyyy-MM-dd} ~ {selectedEndDate:yyyy-MM-dd}",
					MaxPositions = $"{maxPositions} ({maxPositionsType})",
					Leverage = leverage.ToString(),
					RunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
				};

				File.AppendAllText(ReportDirectory.Down($"{reportFileName}.csv"),
					$"{selectedSymbols[0]} +{selectedSymbols.Count - 1},{strategy},{interval1String},{interval2String},{interval3String},{selectedStartDate:yyyy-MM-dd},{selectedEndDate:yyyy-MM-dd},{maxPositionsType},{maxPositions},{leverage},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

				var type = assembly.GetType($"{StrategyNamespace}.{strategy}") ?? throw new Exception($"Strategy type '{StrategyNamespace}.{strategy}' not found.");
				var fields = GetFields(type);

				// Header 생성 및 CSV 파일에 저장
				var paramHeaders = fields.Select(f => f.Name).ToList();
				var headerList = new List<string>();
				headerList.AddRange(paramHeaders);
				headerList.AddRange(["Win", "Lose", "WinRate", "EstimatedMoney", "MDD", "ResultPerRisk"]);
				var headerString = string.Join(",", headerList);
				File.AppendAllText(ReportDirectory.Down($"{reportFileName}.csv"), headerString + Environment.NewLine);

				// 리플렉션 최적화: 메서드/프로퍼티 미리 캐시
				var initMethod = type.GetMethod("Init") ?? throw new Exception("Init not found.");
				var runMethod = type.GetMethod("Run") ?? throw new Exception("Run not found.");
				var winProperty = type.GetProperty("Win");
				var loseProperty = type.GetProperty("Lose");
				var estimatedMoneyProperty = type.GetProperty("EstimatedMoney");
				var mddProperty = type.GetProperty("Mdd");
				var resultPerRiskProperty = type.GetProperty("ResultPerRisk");
				var dailyHistoryProperty = type.GetProperty("IsGenerateDailyHistory");
				var positionHistoryProperty = type.GetProperty("IsGeneratePositionHistory");
				var tradeHistoryProperty = type.GetProperty("IsGenerateTradeHistory");
				var longPositionProperty = type.GetProperty("IsEnableLongPosition");
				var shortPositionProperty = type.GetProperty("IsEnableShortPosition");
				var feeRateProperty = type.GetProperty("FeeRate");

				// 1) 전략 파라미터별 값 리스트 생성 (단일값 또는 범위 "start;step;end" 처리)
				var paramValuesList = new List<List<object>>();

				foreach (var field in fields)
				{
					string text = strategyParameters[field.Name];
					if (string.IsNullOrEmpty(text))
					{
						paramValuesList.Add([GetDefault(field.FieldType)]);
					}
					else if (text.Contains(';'))
					{
						var parts = text.Split(';');
						if (parts.Length == 3 &&
							TryParse(parts[0], field.FieldType, out dynamic? start) &&
							TryParse(parts[1], field.FieldType, out dynamic? step) &&
							TryParse(parts[2], field.FieldType, out dynamic? end))
						{
							var values = new List<object>();
							for (dynamic? val = start; Compare(val, end, field.FieldType) <= 0; val += step)
							{
								values.Add(val);
							}
							paramValuesList.Add(values);
						}
						else
						{
							// 파싱 실패시 단일값으로 처리
							if (TryParse(text, field.FieldType, out var singleVal))
								paramValuesList.Add([singleVal]);
							else
								paramValuesList.Add([GetDefault(field.FieldType)]);
						}
					}
					else
					{
						if (TryParse(text, field.FieldType, out var singleVal))
							paramValuesList.Add([singleVal]);
						else
							paramValuesList.Add([GetDefault(field.FieldType)]);
					}
				}

				// 2) N차원 카르테시안 곱 (조합) 생성
				IEnumerable<List<object>> GetCartesianProduct(List<List<object>> sequences)
				{
					IEnumerable<List<object>> result = [[]];
					foreach (var sequence in sequences)
					{
						result = from accseq in result
								 from item in sequence
								 select new List<object>(accseq) { item };
					}
					return result;
				}

				var combos = GetCartesianProduct(paramValuesList);

				// 3) 완전 순차 처리로 결정적 결과 보장
				var results = new List<string>();
				var comboList = combos.ToList();

				var headerUpdate = new BacktestProgressUpdate { Header = headerList, CsvLine = "" };
				worker.ReportProgress(0, headerUpdate);

				for (int i = 0; i < comboList.Count; i++)
				{
					var combo = comboList[i];
					var backtester = Activator.CreateInstance(type, reportFileName, seed, leverage, maxPositionsType, maxPositions);

					dailyHistoryProperty?.SetValue(backtester, isGenerateDailyHistory);
					positionHistoryProperty?.SetValue(backtester, isGeneratePositionHistory);
					tradeHistoryProperty?.SetValue(backtester, isGenerateTradeHistory); longPositionProperty?.SetValue(backtester, isLongEnable);
					shortPositionProperty?.SetValue(backtester, isShortEnable);
					feeRateProperty?.SetValue(backtester, feeRate);

					for (int j = 0; j < fields.Length; j++)
					{
						fields[j].SetValue(backtester, combo[j]);
					}

					initMethod.Invoke(backtester, [chartPacks, new List<decimal[]>()]);
					runMethod.Invoke(backtester, [selectedStartDate.AddDays(10), selectedEndDate]);

					var win = winProperty?.GetValue(backtester).ToInt() ?? 0;
					var lose = loseProperty?.GetValue(backtester).ToInt() ?? 0;
					var winRate = win + lose > 0 ? (decimal)win / (win + lose) : 0m;
					var estimatedMoney = estimatedMoneyProperty?.GetValue(backtester).ToDecimal() ?? 0m;
					var mdd = mddProperty?.GetValue(backtester).ToDecimal() ?? 0m;
					var resultPerRisk = resultPerRiskProperty?.GetValue(backtester).ToDecimal() ?? 0m;

					if (estimatedMoney <= 0)
					{
						resultPerRisk = 0m;
					}

					string paramStr = string.Join(",", fields.Select((f, idx) => $"{combo[idx]}"));

					string result = $"{paramStr},{win},{lose},{winRate.Round(4):P},{estimatedMoney.Round(0)},{mdd.Round(4):P},{resultPerRisk.Round(4)}";

					results.Add(result);

					var progressUpdate = new BacktestProgressUpdate { CsvLine = result };
					worker.ReportProgress((i + 1) * 100 / comboList.Count, progressUpdate);
				}

				// 4) 결과 파일 저장
				File.AppendAllLines(ReportDirectory.Down($"{reportFileName}.csv"), results);
				File.AppendAllText(ReportDirectory.Down($"{reportFileName}.csv"), "END" + Environment.NewLine + Environment.NewLine);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		#region Regex
		[GeneratedRegex("[0-9]")]
		private static partial Regex NumericRegex();
		[GeneratedRegex("[^0-9]")]
		private static partial Regex NonDigitRegex();
		#endregion

		#region Worker Events
		private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
		{
			if (e.UserState is BacktestProgressUpdate update)
			{
				// Handle header separately - ONLY set header, don't process as data
				if (update.Header != null && update.Header.Count > 0 && currentHeader.Count == 0)
				{
					currentHeader = update.Header;
					// Display run info when we first get header
					if (currentRunInfo != null)
					{
						DisplayRunInfo(currentRunInfo);
					}
					// Always return when setting header - never process header as data
					return;
				}

				// Only add data rows, not headers - and only if header was already set
				if (!string.IsNullOrEmpty(update.CsvLine) && currentHeader.Count > 0)
				{
					AddResultToCurrent(update.CsvLine);
				}

				StatusTextBlock.Text = $"Running backtest... ({e.ProgressPercentage}%)";

				// Update progress bar
				BacktestProgressBar.Value = e.ProgressPercentage;
				BacktestProgressBar.Visibility = Visibility.Visible;
			}
		}

		private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
		{
			worker.DoWork -= Worker_DoWork;
			worker.ProgressChanged -= Worker_ProgressChanged;
			worker.RunWorkerCompleted -= Worker_RunWorkerCompleted;

			if (e.Error != null)
			{
				StatusTextBlock.Text = $"Backtest error: {e.Error.Message}";

				// Hide progress bar on error
				BacktestProgressBar.Visibility = Visibility.Collapsed;
				BacktestProgressBar.Value = 0;
			}
			else
			{
				// 백테스트 완료 시 새로운 페이지로 확정
				FinalizePage();

				// 숫자값에 콤마 포매팅 적용
				ApplyNumberFormatting();

				// 완료 후 파라미터 순으로 정렬
				SortCurrentPageByParameters();

				// 파라미터 통계 생성 및 표시
				GenerateParameterStatistics();

				StatusTextBlock.Text = $"Backtest completed! Total {GetTotalResultCount()} results";

				// Hide progress bar
				BacktestProgressBar.Visibility = Visibility.Collapsed;
				BacktestProgressBar.Value = 0;

				// 파일 모니터링 시작 (파일명이 설정된 경우)
				if (!string.IsNullOrEmpty(reportFileName))
				{
					StartFileMonitoring(Path.Combine(ReportDirectory, $"{reportFileName}.csv"));
				}
			}

			//Environment.Exit(0);
		}
		#endregion

		#region Multi-Page CSV Results Management
		private void AddResultToCurrent(string csvLine)
		{
			var values = csvLine.Split(',');

			// Skip if this line is not actual data
			if (!IsDataLine(values))
			{
				return;
			}

			// Create columns from header if not already created
			if (currentPageData.Columns.Count == 0 && currentHeader.Count > 0)
			{
				// Filter out metadata columns from display
				var displayColumns = GetDisplayColumns();

				// Create DataTable columns
				foreach (var columnName in displayColumns)
				{
					currentPageData.Columns.Add(columnName);
				}

				// Create DataGrid columns manually
				BacktestResultsDataGrid.Columns.Clear();
				foreach (var columnName in displayColumns)
				{
					var column = new DataGridTextColumn
					{
						Header = columnName,
						Binding = new Binding($"[{columnName}]"),
						Width = DataGridLength.Auto
					};

					// Apply text alignment based on column type
					var style = new Style(typeof(TextBlock));
					if (IsNumericColumn(columnName) || IsResultColumn(columnName))
					{
						style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
					}
					else
					{
						style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
					}
					column.ElementStyle = style;

					BacktestResultsDataGrid.Columns.Add(column);
				}

				BacktestResultsDataGrid.ItemsSource = currentPageData.DefaultView;
			}

			// Add row data (only for display columns)
			var displayCols = GetDisplayColumns();
			var row = currentPageData.NewRow();

			for (int i = 0; i < displayCols.Count; i++)
			{
				var originalIndex = currentHeader.IndexOf(displayCols[i]);
				if (originalIndex >= 0 && originalIndex < values.Length)
				{
					row[i] = values[originalIndex].Trim();
				}
			}

			currentPageData.Rows.Add(row);

			// Auto-scroll to last row in real-time
			ScrollToLastRow();

			UpdateStatistics();
			ApplyBacktestResultsColumnAlignment();
		}

		private bool IsHeaderLine(string[] values)
		{
			// Check if this line contains header names instead of data
			if (values.Length > 0)
			{
				// Look for typical header column names (이제 파라미터 이름들도 포함)
				return values.Any(v => v.Trim().Equals("Win", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Equals("Lose", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Equals("WinRate", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Equals("EstimatedMoney", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Equals("MDD", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Equals("ResultPerRisk", StringComparison.OrdinalIgnoreCase) ||
									  // 일반적인 파라미터 이름들도 체크
									  v.Trim().Contains("Period", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Contains("Length", StringComparison.OrdinalIgnoreCase) ||
									  v.Trim().Contains("Threshold", StringComparison.OrdinalIgnoreCase));
			}
			return false;
		}

		private bool IsDataLine(string[] values)
		{
			// Check if this line contains actual data (not header, not metadata)
			if (values.Length == 0) return false;

			// Skip empty lines
			if (values.All(v => string.IsNullOrWhiteSpace(v))) return false;

			// Skip header lines
			if (IsHeaderLine(values)) return false;

			// Skip metadata lines (run info lines with symbols and dates)
			if (IsMetadataLine(values)) return false;

			// Must contain at least some numeric data (in the last few columns which are results)
			var lastFewValues = values.Skip(Math.Max(0, values.Length - 6)).ToArray();
			return lastFewValues.Any(v => decimal.TryParse(v.Trim().Replace(",", "").Replace("%", ""), out _));
		}

		private bool IsMetadataLine(string[] values)
		{
			// Check if this line contains metadata (run info with symbols, dates, etc.)
			if (values.Length > 0)
			{
				var firstValue = values[0].Trim();
				// 메타데이터 라인은 보통 심볼로 시작하고 "+" 기호가 있거나 날짜 형식을 포함
				return firstValue.Contains("+") ||
					   firstValue.Contains("USDT") ||
					   values.Any(v => v.Trim().Contains("-") && v.Trim().Length == 10); // 날짜 형식 체크
			}
			return false;
		}

		private void ApplyBacktestResultsColumnAlignment()
		{
			// This method is no longer needed since we manually create columns with proper alignment
			// Keeping it for compatibility but it's now a no-op
		}

		private void FinalizePage()
		{
			if (currentPageData.Rows.Count > 0)
			{
				// Convert DataTable to List<Dictionary<string, object>>
				var pageData = new List<Dictionary<string, object>>();
				foreach (DataRow row in currentPageData.Rows)
				{
					var dict = new Dictionary<string, object>();
					foreach (DataColumn column in currentPageData.Columns)
					{
						dict[column.ColumnName] = row[column.ColumnName];
					}
					pageData.Add(dict);
				}

				backtestPages.Add(pageData);
				pageHeaders.Add([.. currentHeader]);
				pageRunInfos.Add(currentRunInfo ?? new RunInfo());
				currentPageIndex = backtestPages.Count - 1;
				UpdatePageNavigation();
			}
		}

		private void LoadResultsFromFile(string filePath)
		{
			try
			{
				StopFileMonitoring();
				InitializeDataStructures();

				var fileContent = File.ReadAllText(filePath);
				var pages = fileContent.Split(["END"], StringSplitOptions.RemoveEmptyEntries);

				foreach (var pageContent in pages)
				{
					var lines = pageContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
						.Select(l => l.Trim())
						.Where(l => !string.IsNullOrEmpty(l))
						.ToList();

					if (lines.Count == 0) continue;

					int lineIndex = 0;
					RunInfo? pageRunInfo = null;
					if (lines[0].Contains('+') && lines[0].Contains('-'))
					{
						// Parse run info from the first line
						pageRunInfo = ParseRunInfoFromLine(lines[0]);
						lineIndex++;
					}
					if (lines.Count <= lineIndex) continue;

					List<string> header = [];
					var pageResults = new List<Dictionary<string, object>>();

					// 헤더를 찾기 위해 라인들을 스캔
					for (int i = lineIndex; i < lines.Count; i++)
					{
						var currentLine = lines[i];
						var values = currentLine.Split(',');

						if (IsHeaderLine(values))
						{
							// 헤더를 찾았으면 저장하고 다음 라인부터 데이터 처리
							header = [.. values.Select(h => h.Trim())];
							lineIndex = i + 1;
							break;
						}
						else if (IsDataLine(values))
						{
							// 헤더 없이 바로 데이터가 나오면 기존 형식으로 처리
							header = GenerateHeaderForOldFormat(values.Length);
							lineIndex = i;
							break;
						}
					}

					// 헤더를 찾았으면 데이터 처리
					if (header.Count > 0)
					{
						for (int i = lineIndex; i < lines.Count; i++)
						{
							var values = lines[i].Split(',');

							// 데이터 라인인지 확인
							if (IsDataLine(values))
							{
								var dict = new Dictionary<string, object>();
								for (int j = 0; j < header.Count && j < values.Length; j++)
								{
									dict[header[j]] = values[j].Trim();
								}
								pageResults.Add(dict);
							}
						}
					}

					if (pageResults.Count > 0)
					{
						// Fill missing metadata from first data row if available
						if (pageRunInfo != null && pageResults.Count > 0)
						{
							var firstRow = pageResults[0];
							if (string.IsNullOrEmpty(pageRunInfo.Strategy) && firstRow.ContainsKey("Strategy"))
								pageRunInfo.Strategy = firstRow["Strategy"]?.ToString() ?? "";
						}

						backtestPages.Add(pageResults);
						pageHeaders.Add(header);
						pageRunInfos.Add(pageRunInfo ?? new RunInfo());
					}
				}

				if (backtestPages.Count > 0)
				{
					currentPageIndex = backtestPages.Count - 1;
					LoadCurrentPage();
					StartFileMonitoring(filePath);
				}

				UpdatePageNavigation();
				StatusTextBlock.Text = $"Loaded {backtestPages.Count} pages, {GetTotalResultCount()} total results";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"File load error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private List<string> GenerateHeaderForOldFormat(int columnCount)
		{
			var generatedHeader = new List<string>();
			for (int i = 0; i < columnCount; i++)
			{
				string columnName;
				if (i >= columnCount - 6)
				{
					var fixedColumns = new[] { "Win", "Lose", "WinRate", "EstimatedMoney", "MDD", "ResultPerRisk" };
					int fixedIndex = i - (columnCount - 6);
					columnName = fixedIndex >= 0 && fixedIndex < fixedColumns.Length ? fixedColumns[fixedIndex] : $"Col{i + 1}";
				}
				else
				{
					columnName = $"Param{i + 1}";
				}
				generatedHeader.Add(columnName);
			}
			return generatedHeader;
		}

		private void LoadCurrentPage()
		{
			if (currentPageIndex >= 0 && currentPageIndex < backtestPages.Count)
			{
				currentPageData = new DataTable();
				var headers = pageHeaders[currentPageIndex];
				currentHeader = [.. headers];

				// Load run info if available
				if (currentPageIndex < pageRunInfos.Count)
				{
					currentRunInfo = pageRunInfos[currentPageIndex];
					DisplayRunInfo(currentRunInfo);
				}

				// Add only display columns (exclude metadata columns)
				var displayColumns = GetDisplayColumns();

				// Create DataTable columns
				foreach (var columnName in displayColumns)
				{
					currentPageData.Columns.Add(columnName);
				}

				// Create DataGrid columns manually
				BacktestResultsDataGrid.Columns.Clear();
				foreach (var columnName in displayColumns)
				{
					var column = new DataGridTextColumn
					{
						Header = columnName,
						Binding = new Binding($"[{columnName}]"),
						Width = DataGridLength.Auto
					};

					// Apply text alignment based on column type
					var style = new Style(typeof(TextBlock));
					if (IsParameterColumn(columnName) || IsResultColumn(columnName))
					{
						style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
					}
					else
					{
						style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
					}
					column.ElementStyle = style;

					BacktestResultsDataGrid.Columns.Add(column);
				}

				// Add rows (only display columns)
				foreach (var dict in backtestPages[currentPageIndex])
				{
					var row = currentPageData.NewRow();
					foreach (var columnName in displayColumns)
					{
						row[columnName] = dict.TryGetValue(columnName, out var value) ? value : DBNull.Value;
					}
					currentPageData.Rows.Add(row);
				}

				// 로드된 데이터에 정렬과 포매팅 적용
				ApplyNumberFormatting();
				SortCurrentPageByParameters();
				GenerateParameterStatistics();

				BacktestResultsDataGrid.ItemsSource = currentPageData.DefaultView;
			}

			UpdateStatistics();
			ApplyBacktestResultsColumnAlignment();
		}

		private void UpdatePageNavigation()
		{
			bool hasPages = backtestPages.Count > 0;
			bool hasMultiplePages = backtestPages.Count > 1;

			FirstPageButton.IsEnabled = hasMultiplePages && currentPageIndex > 0;
			PrevPageButton.IsEnabled = hasMultiplePages && currentPageIndex > 0;
			NextPageButton.IsEnabled = hasMultiplePages && currentPageIndex < backtestPages.Count - 1;
			LastPageButton.IsEnabled = hasMultiplePages && currentPageIndex < backtestPages.Count - 1;

			if (hasPages)
			{
				PageInfoTextBlock.Text = $"{currentPageIndex + 1}/{backtestPages.Count}";
			}
			else
			{
				PageInfoTextBlock.Text = "0/0";
			}
		}

		private int GetTotalResultCount()
		{
			return backtestPages.Sum(page => page.Count);
		}

		private void UpdateStatistics()
		{
			var totalResults = GetTotalResultCount();
			var currentPageResultsCount = currentPageData?.Rows.Count ?? 0;

			TotalResultsTextBlock.Text = $"Total: {totalResults}";
			CurrentPageInfoTextBlock.Text = $"Page: {currentPageResultsCount}";
		}

		private void ScrollToLastRow()
		{
			try
			{
				if (BacktestResultsDataGrid.Items.Count > 0)
				{
					var lastItem = BacktestResultsDataGrid.Items[^1];
					BacktestResultsDataGrid.ScrollIntoView(lastItem);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Scroll failed: {ex.Message}");
			}
		}

		private void SortCurrentPageByParameters()
		{
			try
			{
				if (currentPageData?.Rows.Count > 0 && currentHeader.Count > 0)
				{
					// 메타데이터와 결과 컬럼을 제외한 파라미터 컬럼들로 정렬
					var parameterColumns = GetParameterColumns();

					if (parameterColumns.Count > 0)
					{
						// 모든 파라미터 컬럼으로 다중 정렬 (앞에서부터 우선순위)
						var sortColumns = new List<string>();
						foreach (var column in parameterColumns)
						{
							// 컬럼이 실제로 존재하는지 확인
							if (currentPageData.Columns.Contains(column))
							{
								if (IsNumericColumn(column))
								{
									// 숫자 정렬을 위한 안전한 방법
									sortColumns.Add($"[{column}] ASC");
								}
								else
								{
									sortColumns.Add($"[{column}] ASC");
								}
							}
						}

						if (sortColumns.Count > 0)
						{
							var dataView = currentPageData.DefaultView;
							dataView.Sort = string.Join(", ", sortColumns);

							// 숫자 컬럼에 대해서는 별도로 커스텀 정렬 적용
							ApplyCustomSort(dataView, parameterColumns);

							BacktestResultsDataGrid.ItemsSource = dataView;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Sort failed: {ex.Message}");
			}
		}

		private void ApplyCustomSort(DataView dataView, List<string> parameterColumns)
		{
			try
			{
				// 숫자 컬럼들에 대해서는 값으로 정렬
				var numericColumns = parameterColumns.Where(col =>
					currentPageData.Columns.Contains(col) && IsNumericColumn(col)).ToList();

				if (numericColumns.Count > 0)
				{
					var sortedRows = dataView.ToTable().AsEnumerable()
						.OrderBy(row =>
						{
							// 첫 번째 파라미터 컬럼 값으로 정렬
							var firstCol = parameterColumns.FirstOrDefault(col => currentPageData.Columns.Contains(col));
							if (firstCol != null)
							{
								var valueStr = row[firstCol]?.ToString()?.Trim()?.Replace(",", "");
								if (IsNumericColumn(firstCol) && decimal.TryParse(valueStr, out decimal numVal))
									return numVal.ToString("000000000.0000"); // 패딩으로 문자열 정렬
								return valueStr ?? "";
							}
							return "";
						})
						.ThenBy(row =>
						{
							// 두 번째 파라미터 컬럼이 있다면
							var secondCol = parameterColumns.Skip(1).FirstOrDefault(col => currentPageData.Columns.Contains(col));
							if (secondCol != null)
							{
								var valueStr = row[secondCol]?.ToString()?.Trim()?.Replace(",", "");
								if (IsNumericColumn(secondCol) && decimal.TryParse(valueStr, out decimal numVal))
									return numVal.ToString("000000000.0000");
								return valueStr ?? "";
							}
							return "";
						});

					// 새로운 DataTable 생성
					var newTable = currentPageData.Clone();
					foreach (var row in sortedRows)
					{
						newTable.ImportRow(row);
					}

					currentPageData = newTable;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Custom sort failed: {ex.Message}");
			}
		}

		private List<string> GetParameterColumns()
		{
			return currentHeader.Where(h =>
				h != "MaxPositionsType" &&
				h != "MaxPositions" &&
				h != "Leverage" &&
				!h.Contains("Win") &&
				!h.Contains("Lose") &&
				!h.Contains("Rate") &&
				!h.Contains("Money") &&
				!h.Contains("MDD") &&
				!h.Contains("Risk")).ToList();
		}

		private List<string> GetDisplayColumns()
		{
			return [.. currentHeader];
		}

		private bool IsNumericColumn(string columnName)
		{
			if (currentPageData?.Rows.Count > 0 && currentPageData.Columns.Contains(columnName))
			{
				var sampleValue = currentPageData.Rows[0][columnName]?.ToString()?.Trim();
				return decimal.TryParse(sampleValue?.Replace(",", ""), out _);
			}
			return false;
		}

		private bool IsResultColumn(string columnName)
		{
			return columnName.Contains("Win") ||
				   columnName.Contains("Lose") ||
				   columnName.Contains("WinRate") ||
				   columnName.Contains("EstimatedMoney") ||
				   columnName.Contains("MDD") ||
				   columnName.Contains("ResultPerRisk") ||
				   columnName.Contains("Risk");
		}

		private bool IsParameterColumn(string columnName)
		{
			// Parameter columns are numeric but not result columns
			return !IsResultColumn(columnName) &&
				   columnName != "MaxPositionsType" &&
				   columnName != "MaxPositions" &&
				   columnName != "Leverage";
		}

		private void ApplyNumberFormatting()
		{
			try
			{
				if (currentPageData?.Rows.Count > 0 && currentPageData.Columns.Count > 0)
				{
					// 숫자 컬럼들 식별 (퍼센트가 아닌 숫자들)
					var displayColumns = currentPageData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
					var numericColumns = displayColumns.Where(h =>
						!h.Contains("Rate") &&
						!h.Contains("MDD") &&
						IsNumericColumn(h)).ToList();

					foreach (DataRow row in currentPageData.Rows)
					{
						foreach (var column in numericColumns)
						{
							var value = row[column]?.ToString()?.Trim();
							if (!string.IsNullOrEmpty(value) && decimal.TryParse(value.Replace(",", ""), out decimal numValue))
							{
								// 정수는 N0, 소수는 N4 포맷 적용
								if (numValue == Math.Floor(numValue))
								{
									row[column] = numValue.ToString("N0");
								}
								else
								{
									row[column] = numValue.ToString("N4");
								}
							}
						}
					}

					// EstimatedMoney 컬럼 특별 처리 (항상 정수로)
					if (currentPageData.Columns.Contains("EstimatedMoney"))
					{
						foreach (DataRow row in currentPageData.Rows)
						{
							var value = row["EstimatedMoney"]?.ToString()?.Trim();
							if (!string.IsNullOrEmpty(value) && decimal.TryParse(value.Replace(",", ""), out decimal money))
							{
								row["EstimatedMoney"] = money.ToString("N0");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// 포매팅 실패 시 무시 (원본 데이터 유지)
				Debug.WriteLine($"Formatting failed: {ex.Message}");
			}
		}

		private void DisplayRunInfo(RunInfo runInfo)
		{
			RunInfoSymbolText.Text = runInfo.Symbol;
			RunInfoStrategyText.Text = runInfo.Strategy;
			RunInfoIntervalsText.Text = string.IsNullOrEmpty(runInfo.Intervals) ? "None" : runInfo.Intervals;
			RunInfoPeriodText.Text = runInfo.Period;
			RunInfoMaxPosText.Text = runInfo.MaxPositions;
			RunInfoLeverageText.Text = runInfo.Leverage;
			RunInfoTimeText.Text = runInfo.RunTime;
			RunInfoPanel.Visibility = Visibility.Visible;
		}

		private void GenerateParameterStatistics()
		{
			try
			{
				if (currentPageIndex >= 0 && currentPageIndex < backtestPages.Count && currentHeader.Count > 0)
				{
					var parameterColumns = GetParameterColumns();
					var statsList = new List<ParameterValueStats>();

					// Use original data from backtestPages for statistics
					var originalData = backtestPages[currentPageIndex];

					foreach (var paramColumn in parameterColumns)
					{
						// Get unique parameter values and their statistics from original data
						var paramGroups = originalData
							.GroupBy(row => row.TryGetValue(paramColumn, out var val) ? val?.ToString()?.Trim() : "")
							.Where(g => !string.IsNullOrEmpty(g.Key))
							.ToList();

						if (paramGroups.Count > 1) // Only show stats if there are multiple values
						{
							foreach (var group in paramGroups.OrderBy(g =>
							{
								// Try to sort numerically if possible
								if (decimal.TryParse(g.Key?.Replace(",", ""), out decimal numVal))
									return numVal.ToString("000000000.0000");
								return g.Key ?? "";
							}))
							{
								var stats = new ParameterValueStats
								{
									Parameter = paramColumn,
									Value = group.Key ?? ""
								};

								// Calculate averages for this parameter value from original data
								stats.WinRate = CalculateAverageFromDict(group, "WinRate", true);
								stats.Money = CalculateAverageFromDict(group, "EstimatedMoney", false);
								stats.Mdd = CalculateAverageFromDict(group, "MDD", true);
								stats.Risk = CalculateAverageFromDictWithDecimals(group, "ResultPerRisk", 4);

								statsList.Add(stats);
							}
						}
					}

					// Calculate bar width ratios (0-1) based on relative values
					if (statsList.Count > 0)
					{
						// Find max values for each metric
						var maxWinRate = statsList.Max(s => ParseDecimalValue(s.WinRate));
						var maxMoney = statsList.Max(s => ParseDecimalValue(s.Money));
						var maxMdd = statsList.Max(s => ParseDecimalValue(s.Mdd));
						var maxRisk = statsList.Max(s => ParseDecimalValue(s.Risk));

						// Set bar width ratios (0-1) for responsive sizing
						foreach (var stats in statsList)
						{
							var winRateVal = ParseDecimalValue(stats.WinRate);
							var moneyVal = ParseDecimalValue(stats.Money);
							var mddVal = ParseDecimalValue(stats.Mdd);
							var riskVal = ParseDecimalValue(stats.Risk);

							stats.WinRateBarWidth = maxWinRate > 0 ? (double)(winRateVal / maxWinRate) : 0;
							stats.MoneyBarWidth = maxMoney > 0 ? (double)(moneyVal / maxMoney) : 0;
							stats.MddBarWidth = maxMdd > 0 ? (double)(mddVal / maxMdd) : 0;
							stats.RiskBarWidth = maxRisk > 0 ? (double)(riskVal / maxRisk) : 0;
						}
					}

					StatsDataGrid.ItemsSource = statsList;
					StatsPanel.Visibility = statsList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Stats generation failed: {ex.Message}");
			}
		}

		private string CalculateAverage(IGrouping<string?, DataRow> group, string columnName, bool isPercentage)
		{
			try
			{
				if (!currentHeader.Contains(columnName)) return "N/A";

				var values = group.Select(row =>
				{
					var valueStr = row[columnName]?.ToString()?.Trim()?.Replace(",", "").Replace("%", "");
					return decimal.TryParse(valueStr, out decimal val) ? val : (decimal?)null;
				}).Where(v => v.HasValue).Select(v => v!.Value).ToList();

				if (values.Count > 0)
				{
					var avg = values.Count > 0 ? values.Average() : 0m;
					if (isPercentage)
					{
						return avg.ToString("P2");
					}
					else
					{
						return avg.ToString("N0");
					}
				}
			}
			catch { }
			return "N/A";
		}

		private string CalculateAverageFromDict(IGrouping<string?, Dictionary<string, object>> group, string columnName, bool isPercentage)
		{
			try
			{
				if (!currentHeader.Contains(columnName)) return "N/A";

				var values = group.Select(dict =>
				{
					if (dict.TryGetValue(columnName, out var val))
					{
						var valueStr = val?.ToString()?.Trim()?.Replace(",", "").Replace("%", "");
						return decimal.TryParse(valueStr, out decimal numVal) ? numVal : (decimal?)null;
					}
					return null;
				}).Where(v => v.HasValue).Select(v => v!.Value).ToList();

				if (values.Count > 0)
				{
					var avg = values.Average();
					if (isPercentage)
					{
						// Check if value is already in percentage format (>1) or decimal format (<1)
						if (avg <= 1.0m)
						{
							// Decimal format (0.65), convert to percentage
							return (avg * 100).ToString("F2") + "%";
						}
						else
						{
							// Already percentage format (65%), use as-is
							return avg.ToString("F2") + "%";
						}
					}
					else
					{
						return avg.ToString("N0");
					}
				}
			}
			catch { }
			return "N/A";
		}

		private string CalculateAverageFromDictWithDecimals(IGrouping<string?, Dictionary<string, object>> group, string columnName, int decimalPlaces)
		{
			try
			{
				if (!currentHeader.Contains(columnName)) return "N/A";

				var values = group.Select(dict =>
				{
					if (dict.TryGetValue(columnName, out var val))
					{
						var valueStr = val?.ToString()?.Trim()?.Replace(",", "");
						return decimal.TryParse(valueStr, out decimal numVal) ? numVal : (decimal?)null;
					}
					return null;
				}).Where(v => v.HasValue).Select(v => v!.Value).ToList();

				if (values.Count > 0)
				{
					var avg = values.Average();
					return avg.ToString($"F{decimalPlaces}");
				}
			}
			catch { }
			return "N/A";
		}

		private decimal ParseDecimalValue(string value)
		{
			if (string.IsNullOrEmpty(value) || value == "N/A") return 0;

			var cleanValue = value.Replace(",", "").Replace("%", "").Trim();
			return decimal.TryParse(cleanValue, out decimal result) ? Math.Abs(result) : 0;
		}

		private List<Dictionary<string, string>> LoadDailyData(string reportFileName)
		{
			var dailyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{reportFileName}_daily.csv");
			var dailyData = new List<Dictionary<string, string>>();

			try
			{
				Debug.WriteLine($"Looking for daily file: {dailyPath}");

				if (File.Exists(dailyPath))
				{
					var lines = File.ReadAllLines(dailyPath);
					Debug.WriteLine($"Daily file found, {lines.Length} lines");

					string[] headers = null;
					int lastHeaderIndex = -1;

					// Find the most recent header (last occurrence)
					for (int i = lines.Length - 1; i >= 0; i--)
					{
						var line = lines[i].Trim();
						if (line.Contains("DateTime") && line.Contains(','))
						{
							headers = line.Split(',').Select(h => h.Trim()).ToArray();
							lastHeaderIndex = i;
							Debug.WriteLine($"Found daily headers at line {i}: {string.Join(", ", headers)}");
							break;
						}
					}

					if (headers != null && lastHeaderIndex >= 0)
					{
						// Process only lines after the last header
						for (int i = lastHeaderIndex + 1; i < lines.Length; i++)
						{
							var line = lines[i].Trim();
							if (string.IsNullOrEmpty(line)) continue;

							// Skip special markers
							if (line == "LIQ" || line.Contains("MDD:") || line.Contains("Monday:")) continue;

							var parts = line.Split(',');
							if (parts.Length >= headers.Length)
							{
								try
								{
									var row = new Dictionary<string, string>();
									for (int j = 0; j < headers.Length && j < parts.Length; j++)
									{
										var value = parts[j].Trim();
										// Apply number formatting for numeric values
										if (decimal.TryParse(value.Replace(",", "").Replace("%", ""), out decimal numVal))
										{
											if (headers[j] == "WinRate")
											{
												row[headers[j]] = value.Contains('%') ? value : numVal.ToString("F2") + "%";
											}
											else if (headers[j].Contains("Per") || headers[j].Contains("Rate"))
											{
												row[headers[j]] = value.Contains('%') ? value : numVal.ToString("P2");
											}
											else if (headers[j] == "Win" || headers[j] == "Lose" || headers[j].Contains("Count"))
											{
												row[headers[j]] = numVal.ToString("N0");
											}
											else
											{
												row[headers[j]] = numVal.ToString("N0");
											}
										}
										else
										{
											row[headers[j]] = value;
										}
									}
									dailyData.Add(row);
								}
								catch (Exception parseEx)
								{
									Debug.WriteLine($"Error parsing daily line {i}: {parseEx.Message}");
								}
							}
						}
					}
				}
				else
				{
					Debug.WriteLine($"Daily file not found: {dailyPath}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading daily data: {ex.Message}");
			}

			Debug.WriteLine($"Loaded {dailyData.Count} daily records");
			return dailyData;
		}

		private List<Dictionary<string, string>> LoadPositionData(string reportFileName)
		{
			var positionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{reportFileName}_position.csv");
			var positionData = new List<Dictionary<string, string>>();

			try
			{
				Debug.WriteLine($"Looking for position file: {positionPath}");

				if (File.Exists(positionPath))
				{
					// Store for progressive loading
					currentPositionFile = positionPath;
					allPositionLines = File.ReadAllLines(positionPath);
					Debug.WriteLine($"Position file found, {allPositionLines.Length} lines");

					// Find the most recent header (last occurrence)
					for (int i = allPositionLines.Length - 1; i >= 0; i--)
					{
						var line = allPositionLines[i].Trim();
						if (line.Contains("EntryTime") && line.Contains(','))
						{
							positionHeaders = [.. line.Split(',').Select(h => h.Trim())];
							positionHeaderIndex = i;
							Debug.WriteLine($"Found position headers at line {i}: {string.Join(", ", positionHeaders)}");
							break;
						}
					}

					if (positionHeaders != null && positionHeaderIndex >= 0)
					{
						// 초기 1000개 로드 - ObservableCollection 사용
						currentLoadedPositions = 0;
						positionData = LoadMorePositionData(1000);
						positionDataCollection = new ObservableCollection<Dictionary<string, string>>(positionData);
						positionData = positionDataCollection.ToList(); // 호환성을 위해 List도 반환
					}
				}
				else
				{
					Debug.WriteLine($"Position file not found: {positionPath}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading position data: {ex.Message}");
			}

			Debug.WriteLine($"Loaded {positionData.Count} position records");
			return positionData;
		}

		private List<Dictionary<string, string>> LoadMorePositionData(int maxRecords)
		{
			var positionData = new List<Dictionary<string, string>>();

			if (allPositionLines == null || positionHeaders == null || positionHeaderIndex < 0)
				return positionData;

			int recordCount = 0;
			int startIndex = positionHeaderIndex + 1 + currentLoadedPositions;

			for (int i = startIndex; i < allPositionLines.Length && recordCount < maxRecords; i++)
			{
				var line = allPositionLines[i].Trim();
				if (string.IsNullOrEmpty(line)) continue;

				var parts = line.Split(',');

				if (parts.Length >= positionHeaders.Length)
				{
					try
					{
						var record = new Dictionary<string, string>();
						for (int j = 0; j < positionHeaders.Length && j < parts.Length; j++)
						{
							var value = parts[j].Trim();

							// Apply thousand separator formatting for numeric values
							if (decimal.TryParse(value, out decimal numValue))
							{
								value = numValue.ToString("N0");
							}

							record[positionHeaders[j]] = value;
						}
						positionData.Add(record);
						recordCount++;
						currentLoadedPositions++;
					}
					catch (Exception parseEx)
					{
						Debug.WriteLine($"Error parsing position line {i}: {parseEx.Message}");
					}
				}
			}

			return positionData;
		}

		private void PositionDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (isLoadingMorePositions) return;

			var scrollViewer = e.OriginalSource as ScrollViewer;
			if (scrollViewer != null)
			{
				// 스크롤이 끝에 도달했는지 확인 (90% 이상)
				if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight * 0.9)
				{
					LoadMorePositionsAsync();
				}
			}
		}

		private async void LoadMorePositionsAsync()
		{
			if (isLoadingMorePositions || allPositionLines == null || positionHeaders == null)
				return;

			isLoadingMorePositions = true;

			try
			{
				await Task.Run(() =>
				{
					var moreData = LoadMorePositionData(1000);
					if (moreData.Count > 0)
					{
						Dispatcher.Invoke(() =>
						{
							if (positionDataCollection != null)
							{
								foreach (var item in moreData)
								{
									positionDataCollection.Add(item);
								}

								// UI 업데이트
								DetailsExpander.Header = $"Details ({positionDataCollection.Count} positions loaded, {allPositionLines.Length - positionHeaderIndex - 1 - currentLoadedPositions} remaining)";
							}
						});
					}
				});
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading more positions: {ex.Message}");
			}
			finally
			{
				isLoadingMorePositions = false;
			}
		}

		private RunInfo? ParseRunInfoFromLine(string line)
		{
			try
			{
				// Expected format: "ETCUSDT +4,Strategy,15m,None,None,2023-01-01,2024-12-31,Total,5,10,2025-09-19 10:00:20"
				var parts = line.Split(',');
				if (parts.Length >= 10)
				{
					var symbolPart = parts[0].Trim();
					var strategy = parts[1].Trim();
					var interval1 = parts[2].Trim();
					var interval2 = parts[3].Trim();
					var interval3 = parts[4].Trim();
					var startDate = parts[5].Trim();
					var endDate = parts[6].Trim();
					var maxPositionsType = parts[7].Trim();
					var maxPositions = parts[8].Trim();
					var leverage = parts[9].Trim();
					var runTime = parts.Length > 10 ? parts[10].Trim() : "";

					// Parse intervals
					var intervals = new[] { interval1, interval2, interval3 }
						.Where(i => !string.IsNullOrEmpty(i) && i != "None")
						.ToList();

					return new RunInfo
					{
						Symbol = symbolPart,
						Strategy = strategy,
						Intervals = intervals.Count > 0 ? string.Join(", ", intervals) : "None",
						Period = $"{startDate} ~ {endDate}",
						MaxPositions = $"{maxPositions} ({maxPositionsType})",
						Leverage = leverage,
						RunTime = runTime
					};
				}
				else if (parts.Length >= 9)
				{
					// Legacy format without strategy
					var symbolPart = parts[0].Trim();
					var interval1 = parts[1].Trim();
					var interval2 = parts[2].Trim();
					var interval3 = parts[3].Trim();
					var startDate = parts[4].Trim();
					var endDate = parts[5].Trim();
					var maxPositionsType = parts[6].Trim();
					var maxPositions = parts[7].Trim();
					var leverage = parts[8].Trim();
					var runTime = parts.Length > 9 ? parts[9].Trim() : "";

					// Parse intervals
					var intervals = new[] { interval1, interval2, interval3 }
						.Where(i => !string.IsNullOrEmpty(i) && i != "None")
						.ToList();

					return new RunInfo
					{
						Symbol = symbolPart,
						Strategy = "", // Will be filled from the first data row
						Intervals = intervals.Count > 0 ? string.Join(", ", intervals) : "None",
						Period = $"{startDate} ~ {endDate}",
						MaxPositions = $"{maxPositions} ({maxPositionsType})",
						Leverage = leverage,
						RunTime = runTime
					};
				}
				else if (parts.Length >= 6)
				{
					// Legacy format without metadata
					var symbolPart = parts[0].Trim();
					var interval1 = parts[1].Trim();
					var interval2 = parts[2].Trim();
					var interval3 = parts[3].Trim();
					var startDate = parts[4].Trim();
					var endDate = parts[5].Trim();
					var runTime = parts.Length > 6 ? parts[6].Trim() : "";

					// Parse intervals
					var intervals = new[] { interval1, interval2, interval3 }
						.Where(i => !string.IsNullOrEmpty(i) && i != "None")
						.ToList();

					return new RunInfo
					{
						Symbol = symbolPart,
						Strategy = "", // Will be filled from the first data row
						Intervals = intervals.Count > 0 ? string.Join(", ", intervals) : "None",
						Period = $"{startDate} ~ {endDate}",
						MaxPositions = "", // Will be filled from the first data row
						Leverage = "", // Will be filled from the first data row
						RunTime = runTime
					};
				}
			}
			catch
			{
				// Ignore parsing errors
			}
			return null;
		}

		private void BacktestResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (BacktestResultsDataGrid.SelectedItem is DataRowView selectedRow)
				{
					LoadRowDetails(selectedRow);
				}
				else
				{
					DetailsPanel.Visibility = Visibility.Collapsed;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Selection changed error: {ex.Message}");
			}
		}

		private void LoadRowDetails(DataRowView rowView)
		{
			try
			{
				// Extract reportFileName from the row data
				string reportFileName = ExtractReportFileName(rowView);

				if (!string.IsNullOrEmpty(reportFileName))
				{
					var dailyData = LoadDailyData(reportFileName);
					var positionData = LoadPositionData(reportFileName);

					// Build dynamic columns for Daily DataGrid
					DailyDataGrid.Columns.Clear();
					if (dailyData.Count > 0)
					{
						var firstRecord = dailyData.First();
						foreach (var kvp in firstRecord)
						{
							var column = new DataGridTextColumn
							{
								Header = kvp.Key,
								Binding = new Binding($"[{kvp.Key}]"),
								FontSize = 11.5,
								Width = DataGridLength.Auto
							};

							// Apply consistent styling with text alignment
							var style = new Style(typeof(TextBlock));
							style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.5));

							// Apply text alignment based on data type
							var headerName = kvp.Key;
							if (IsNumericColumnForAlignment(headerName))
							{
								style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
							}
							else
							{
								style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
							}

							column.ElementStyle = style;
							DailyDataGrid.Columns.Add(column);
						}
					}

					// Build dynamic columns for Position DataGrid
					PositionDataGrid.Columns.Clear();
					if (positionData.Count > 0)
					{
						var firstRecord = positionData.First();
						foreach (var kvp in firstRecord)
						{
							var column = new DataGridTextColumn
							{
								Header = kvp.Key,
								Binding = new Binding($"[{kvp.Key}]"),
								FontSize = 11.5,
								Width = DataGridLength.Auto
							};

							// Apply consistent styling with text alignment
							var style = new Style(typeof(TextBlock));
							style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.5));

							// Apply text alignment based on data type
							var headerName = kvp.Key;
							if (IsNumericColumnForAlignment(headerName))
							{
								style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
							}
							else
							{
								style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
							}

							column.ElementStyle = style;
							PositionDataGrid.Columns.Add(column);
						}
					}

					// Update the details grids
					DailyDataGrid.ItemsSource = dailyData;
					PositionDataGrid.ItemsSource = positionDataCollection ?? new ObservableCollection<Dictionary<string, string>>(positionData);

					// Show details panel if we have data
					if (dailyData.Count > 0 || positionData.Count > 0)
					{
						DetailsPanel.Visibility = Visibility.Visible;
						DetailsExpander.Header = $"Details for {reportFileName} ({dailyData.Count} daily, {positionData.Count} positions)";
						DetailsExpander.IsExpanded = true; // Auto-expand when data is loaded
					}
					else
					{
						DetailsPanel.Visibility = Visibility.Collapsed;
					}
				}
				else
				{
					DetailsPanel.Visibility = Visibility.Collapsed;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Load row details error: {ex.Message}");
				DetailsPanel.Visibility = Visibility.Collapsed;
			}
		}

		private string ExtractReportFileName(DataRowView rowView)
		{
			try
			{
				// Use the currently set reportFileName from UI
				var currentReportFileName = FileNameTextBox.Text.Trim();
				if (!string.IsNullOrEmpty(currentReportFileName))
				{
					Debug.WriteLine($"Using current report filename: {currentReportFileName}");
					return currentReportFileName;
				}

				// Fallback: Look for files in Desktop directory
				var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				var csvFiles = Directory.GetFiles(desktopPath, "*_daily.csv")
					.Concat(Directory.GetFiles(desktopPath, "*_position.csv"))
					.OrderByDescending(f => File.GetLastWriteTime(f))
					.ToArray();

				Debug.WriteLine($"Found {csvFiles.Length} CSV files in desktop");

				if (csvFiles.Length > 0)
				{
					// Return the most recently modified file's basename
					var fileName = Path.GetFileNameWithoutExtension(csvFiles[0]);
					var reportName = fileName.Replace("_daily", "").Replace("_position", "");
					Debug.WriteLine($"Using report name: {reportName}");
					return reportName;
				}

				// Fallback: create a test file name
				return "TEST_REPORT_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Extract report filename error: {ex.Message}");
			}

			return string.Empty;
		}

		#region Page Navigation Events
		private void FirstPageButton_Click(object sender, RoutedEventArgs e)
		{
			if (backtestPages.Count > 0)
			{
				currentPageIndex = 0;
				LoadCurrentPage();
				UpdatePageNavigation();
			}
		}

		private void PrevPageButton_Click(object sender, RoutedEventArgs e)
		{
			if (currentPageIndex > 0)
			{
				currentPageIndex--;
				LoadCurrentPage();
				UpdatePageNavigation();
			}
		}

		private void NextPageButton_Click(object sender, RoutedEventArgs e)
		{
			if (currentPageIndex < backtestPages.Count - 1)
			{
				currentPageIndex++;
				LoadCurrentPage();
				UpdatePageNavigation();
			}
		}

		private void LastPageButton_Click(object sender, RoutedEventArgs e)
		{
			if (backtestPages.Count > 0)
			{
				currentPageIndex = backtestPages.Count - 1;
				LoadCurrentPage();
				UpdatePageNavigation();
			}
		}
		#endregion

		#region File Operations
		private void LoadResultsButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var openFileDialog = new OpenFileDialog
				{
					Title = "Select Backtest Results File",
					Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
					InitialDirectory = ReportDirectory
				};

				if (openFileDialog.ShowDialog() == true)
				{
					LoadResultsFromFile(openFileDialog.FileName);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"File load error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
		{
			StopFileMonitoring();
			InitializeDataStructures();
			StatusTextBlock.Text = "All results cleared.";
		}

		private void CopyResultsButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (BacktestResultsDataGrid.Items.Count == 0)
				{
					StatusTextBlock.Text = "No data to copy.";
					return;
				}

				var sb = new StringBuilder();

				// Get visible columns only
				var visibleColumns = BacktestResultsDataGrid.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();

				// Add headers
				var headers = visibleColumns.Select(c => c.Header?.ToString() ?? "").ToArray();
				sb.AppendLine(string.Join("\t", headers));

				// Get data from DataGrid items
				foreach (var item in BacktestResultsDataGrid.Items)
				{
					if (item == null) continue;

					var values = new List<string>();
					foreach (var column in visibleColumns)
					{
						string value = "";

						if (column is DataGridBoundColumn boundColumn)
						{
							var binding = boundColumn.Binding as Binding;
							if (binding != null)
							{
								var propertyPath = binding.Path.Path;

								// Handle DataRowView with indexer binding like [ColumnName]
								if (item is DataRowView rowView)
								{
									try
									{
										// Check if it's an indexer binding [ColumnName]
										if (propertyPath.StartsWith("[") && propertyPath.EndsWith("]"))
										{
											var columnName = propertyPath.Substring(1, propertyPath.Length - 2);
											if (rowView.Row.Table.Columns.Contains(columnName))
											{
												var cellValue = rowView[columnName];
												value = cellValue != null && cellValue != DBNull.Value ? cellValue.ToString() : "";
											}
										}
										else if (rowView.Row.Table.Columns.Contains(propertyPath))
										{
											var cellValue = rowView[propertyPath];
											value = cellValue != null && cellValue != DBNull.Value ? cellValue.ToString() : "";
										}
									}
									catch { }
								}
								// Handle direct object
								else
								{
									try
									{
										var property = item.GetType().GetProperty(propertyPath);
										if (property != null)
										{
											var cellValue = property.GetValue(item);
											value = cellValue?.ToString() ?? "";
										}
									}
									catch { }
								}
							}
						}

						values.Add(value);
					}
					sb.AppendLine(string.Join("\t", values));
				}

				Clipboard.SetText(sb.ToString());
				StatusTextBlock.Text = $"Copied {BacktestResultsDataGrid.Items.Count} rows to clipboard.";
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"Copy error: {ex.Message}";
			}
		}
		#endregion

		#region File Monitoring
		private void StartFileMonitoring(string filePath)
		{
			StopFileMonitoring();

			try
			{
				monitoredFilePath = filePath;
				var directory = Path.GetDirectoryName(filePath);
				var fileName = Path.GetFileName(filePath);

				if (!string.IsNullOrEmpty(directory) && File.Exists(filePath))
				{
					fileWatcher = new FileSystemWatcher(directory, fileName)
					{
						NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
						EnableRaisingEvents = true
					};

					fileWatcher.Changed += OnFileChanged;
				}
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"File monitoring failed: {ex.Message}";
			}
		}

		private void StopFileMonitoring()
		{
			if (fileWatcher != null)
			{
				fileWatcher.EnableRaisingEvents = false;
				fileWatcher.Changed -= OnFileChanged;
				fileWatcher.Dispose();
				fileWatcher = null;
			}

			monitoredFilePath = null;
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (e.FullPath == monitoredFilePath)
			{
				// Delay to ensure file write is complete
				Thread.Sleep(500);

				Dispatcher.BeginInvoke(() =>
				{
					try
					{
						LoadResultsFromFile(e.FullPath);
					}
					catch (Exception ex)
					{
						StatusTextBlock.Text = $"Auto-refresh failed: {ex.Message}";
					}
				});
			}
		}
		private bool IsNumericColumnForAlignment(string columnName)
		{
			// Numeric columns (right-aligned)
			var numericColumns = new[] {
				"Win", "Lose", "WinRate", "EstimatedMoney", "Change", "ChangePer", "MaxPer",
				"Long", "Short", "Income", "Fee", "Result"
			};

			return numericColumns.Contains(columnName) ||
				   columnName.Contains("Amount") ||
				   columnName.Contains("Count") ||
				   columnName.Contains("Money") ||
				   columnName.Contains("Rate") ||
				   columnName.Contains("Per");
		}

		#endregion

		#endregion
	}
}