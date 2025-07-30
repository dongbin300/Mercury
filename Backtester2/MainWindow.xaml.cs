using Backtester2.Apis;

using Mercury;
using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Backtester2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true };

		private int selectedIndex = -1;
		private Assembly assembly = Assembly.LoadFrom("Mercury.dll");
		private readonly string StrategyNamespace = "Mercury.Backtests.BacktestStrategies";
		private readonly string ReportDirectory = MercuryPath.Desktop;

		private SmartRandom random = new SmartRandom();

		public MainWindow()
		{
			InitializeComponent();

			LocalStorageApi.Init();

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
			SingleSymbolCheckBox.IsChecked = Settings.Default.SingleSymbol;
			RandomSymbolCheckBox.IsChecked = Settings.Default.RandomSymbol;
			RandomSymbolCountTextBox.Text = Settings.Default.RandomSymbolCount.Trim();
			RandomDateCheckBox.IsChecked = Settings.Default.RandomDate;
			RandomDatePeriodTextBox.Text = Settings.Default.RandomDatePeriod.Trim();
			LongEnableCheckBox.IsChecked = Settings.Default.LongEnable;
			ShortEnableCheckBox.IsChecked = Settings.Default.ShortEnable;

			var strategyNames = assembly.GetTypes().Where(t => t.IsClass && t.IsPublic && !t.Name.Contains('`') && !t.Name.Contains('<') && t.Namespace == StrategyNamespace).Select(t => t.Name).ToList();
			StrategyComboBox.ItemsSource = strategyNames;

			var intervals = new List<string>() { "None", "1m", "5m", "15m", "30m", "1h", "2h", "4h", "1D" };
			Interval1ComboBox.ItemsSource = intervals;
			Interval2ComboBox.ItemsSource = intervals;
			Interval3ComboBox.ItemsSource = intervals;
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
			Settings.Default.SingleSymbol = SingleSymbolCheckBox.IsChecked == true;
			Settings.Default.RandomSymbol = RandomSymbolCheckBox.IsChecked == true;
			Settings.Default.RandomSymbolCount = RandomSymbolCountTextBox.Text.Trim();
			Settings.Default.RandomDate = RandomDateCheckBox.IsChecked == true;
			Settings.Default.RandomDatePeriod = RandomDatePeriodTextBox.Text.Trim();
			Settings.Default.LongEnable = LongEnableCheckBox.IsChecked == true;
			Settings.Default.ShortEnable = ShortEnableCheckBox.IsChecked == true;
			Settings.Default.Save();
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
				SymbolFilteredListBox.Visibility = Visibility.Collapsed;
				SymbolFilteredListBox.Items.Clear();
				selectedIndex = -1;
				return;
			}

			var filtered = LocalStorageApi.Symbols.Where(i => i.Item1.ToLower().Contains(text, StringComparison.CurrentCultureIgnoreCase)).ToList();

			SymbolFilteredListBox.Items.Clear();
			foreach (var item in filtered)
			{
				string display = $"{item.Item1};{item.Item2,20:yyyy-MM-dd} ~ {item.Item3:yyyy-MM-dd}";
				SymbolFilteredListBox.Items.Add(display);
			}

			if (filtered.Count > 0)
			{
				SymbolFilteredListBox.Visibility = Visibility.Visible;
				selectedIndex = 0;
				SymbolFilteredListBox.SelectedIndex = selectedIndex;
			}
			else
			{
				SymbolFilteredListBox.Visibility = Visibility.Collapsed;
				selectedIndex = -1;
			}
		}

		private void SymbolInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (SymbolFilteredListBox.Visibility != Visibility.Visible || SymbolFilteredListBox.Items.Count == 0)
				return;

			if (e.Key == Key.Down)
			{
				selectedIndex++;
				if (selectedIndex >= SymbolFilteredListBox.Items.Count)
					selectedIndex = 0;

				SymbolFilteredListBox.SelectedIndex = selectedIndex;
				SymbolFilteredListBox.ScrollIntoView(SymbolFilteredListBox.SelectedItem);
				e.Handled = true;
			}
			else if (e.Key == Key.Up)
			{
				selectedIndex--;
				if (selectedIndex < 0)
					selectedIndex = SymbolFilteredListBox.Items.Count - 1;

				SymbolFilteredListBox.SelectedIndex = selectedIndex;
				SymbolFilteredListBox.ScrollIntoView(SymbolFilteredListBox.SelectedItem);
				e.Handled = true;
			}
			else if (e.Key == Key.Enter)
			{
				if (selectedIndex >= 0 && selectedIndex < SymbolFilteredListBox.Items.Count)
				{
					var selectedDisplay = SymbolFilteredListBox.Items[selectedIndex].ToString();
					var selectedSymbol = selectedDisplay?.Split(';')[0] ?? string.Empty;

					var currentSymbols = GetSymbols();
					if (currentSymbols.Any(x => x.Equals(selectedSymbol)))
					{
						MessageBox.Show("이미 존재하는 심볼입니다.");
						return;
					}

					AddSymbolTicker(selectedSymbol);

					SymbolInputTextBox.Clear();
					SymbolFilteredListBox.Items.Clear();
					SymbolFilteredListBox.Visibility = Visibility.Collapsed;
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
				};
				var textbox = new TextBox
				{
					Name = "SP_" + field.Name,
					Text = fieldValue?.ToString() ?? string.Empty,
					Tag = field.Name,
				};
				StrategyParameterPanel.Children.Add(textblock);
				StrategyParameterPanel.Children.Add(textbox);
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
		bool isSingleSymbol = false;
		bool isRandomSymbol = false;
		int randomSymbolCount = 0;
		bool isRandomDate = false;
		int randomDatePeriod = 0;
		bool isLongEnable = false;
		bool isShortEnable = false;

		private void RunButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SaveSettings();

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
				isGenerateDailyHistory = ReportDailyHistoryCheckBox.IsChecked == true;
				isGeneratePositionHistory = ReportPositionHistoryCheckBox.IsChecked == true;
				isSingleSymbol = SingleSymbolCheckBox.IsChecked == true;
				isRandomSymbol = RandomSymbolCheckBox.IsChecked == true;
				randomSymbolCount = RandomSymbolCountTextBox.Text.ToInt();
				isRandomDate = RandomDateCheckBox.IsChecked == true;
				randomDatePeriod = RandomDatePeriodTextBox.Text.ToInt();
				isLongEnable = LongEnableCheckBox.IsChecked == true;
				isShortEnable = ShortEnableCheckBox.IsChecked == true;

				worker.DoWork += Worker_DoWork;
				worker.RunWorkerCompleted += (s, e) =>
				{
					worker.DoWork -= Worker_DoWork;
				};
				worker.RunWorkerAsync();
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
				int count = 0;
				List<string> selectedSymbols = symbols;
				DateTime selectedStartDate = startDate;
				DateTime selectedEndDate = endDate;
				List<ChartPack> chartPacks = [];
				ChartLoader.Charts = [];

				// 심볼 선택
				selectedSymbols =
					isSingleSymbol ? isRandomSymbol ? [random.Next(symbols)] : [symbols[count]]
					: isRandomSymbol ? random.Next(symbols, randomSymbolCount) : symbols;

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

				void LoadChartPacks(string? intervalString, List<ChartPack> chartPacks)
				{
					if (!string.IsNullOrEmpty(intervalString) && intervalString != "None")
					{
						foreach (var symbol in selectedSymbols)
						{
							var chartPack = ChartLoader.InitCharts(symbol, intervalString.ToKlineInterval(), selectedStartDate, selectedEndDate);
							chartPacks.Add(chartPack);
						}
					}
				}

				LoadChartPacks(interval1String, chartPacks);
				LoadChartPacks(interval2String, chartPacks);
				LoadChartPacks(interval3String, chartPacks);

				File.AppendAllText(ReportDirectory.Down($"{reportFileName}.csv"),
					$"{selectedSymbols[0]} +{selectedSymbols.Count - 1},{interval1String},{interval2String},{interval3String},{selectedStartDate:yyyy-MM-dd},{selectedEndDate:yyyy-MM-dd},{DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine);

				var results = new ConcurrentBag<string>();


				var type = assembly.GetType($"{StrategyNamespace}.{strategy}") ?? throw new Exception($"Strategy type '{StrategyNamespace}.{strategy}' not found.");
				var backtester = Activator.CreateInstance(type, reportFileName, seed, leverage, maxPositionsType, maxPositions);

				type.GetProperty("IsGenerateDailyHistory")?.SetValue(backtester, isGenerateDailyHistory);
				type.GetProperty("IsGeneratePositionHistory")?.SetValue(backtester, isGeneratePositionHistory);
				type.GetProperty("IsEnableLongPosition")?.SetValue(backtester, isLongEnable);
				type.GetProperty("IsEnableShortPosition")?.SetValue(backtester, isShortEnable);
				type.GetProperty("FeeRate")?.SetValue(backtester, feeRate);

				var fields = GetFields(type);

				DispatcherService.Invoke(() =>
				{
					foreach (var field in fields)
					{
						var textBox = StrategyParameterPanel.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == "SP_" + field.Name);
						if (textBox == null) continue;

						string input = textBox.Text;
						object? convertedValue = null;
						try
						{
							convertedValue = Convert.ChangeType(input, field.FieldType);
						}
						catch
						{
							convertedValue = null;
						}

						if (convertedValue != null)
						{
							field.SetValue(backtester, convertedValue);
						}
					}
				});

				var initMethod = type.GetMethod("Init") ?? throw new Exception("Init not found.");
				initMethod.Invoke(backtester, [chartPacks, interval1String?.ToKlineInterval(), Array.Empty<decimal>()]);

				var runMethod = type.GetMethod("Run") ?? throw new Exception("Run not found.");
				runMethod.Invoke(backtester, [selectedStartDate.AddDays(10), selectedEndDate]); // TODO AddDays변수로

				var win = type.GetProperty("Win")?.GetValue(backtester).ToInt() ?? 0;
				var lose = type.GetProperty("Lose")?.GetValue(backtester).ToInt() ?? 0;
				var winRate = win + lose > 0 ? (decimal)win / (win + lose) : 0;
				var estimatedMoney = type.GetProperty("EstimatedMoney")?.GetValue(backtester).ToDecimal() ?? 0m;
				var mdd = type.GetProperty("Mdd")?.GetValue(backtester).ToDecimal() ?? 0m;
				var resultPerRisk = type.GetProperty("ResultPerRisk")?.GetValue(backtester).ToDecimal() ?? 0m;

				string result = $"{strategy},{maxPositionsType},{maxPositions},{leverage},{win},{lose},{winRate.Round(4):P},{estimatedMoney.Round(0)},{mdd.Round(4):P},{resultPerRisk.Round(4)}";

				results.Add(result);

				File.AppendAllLines(ReportDirectory.Down($"{reportFileName}.csv"), results);
				File.AppendAllText(ReportDirectory.Down($"{reportFileName}.txt"), "END" + Environment.NewLine + Environment.NewLine + Environment.NewLine);
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
	}
}