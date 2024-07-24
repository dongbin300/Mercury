using Mercury;

using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BacktestViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<Backtest> currentBacktests = [];
		List<BacktestEvent> currentBacktestEvents = [];

		public MainWindow()
		{
			InitializeComponent();

			var csvFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "*.csv");

			foreach (string csvFile in csvFiles)
			{
				var item = new ListBoxItem
				{
					Content = Path.GetFileName(csvFile),
					Tag = csvFile
				};
				FileListBox.Items.Add(item);
			}
		}

		private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (FileListBox.SelectedItem is not ListBoxItem selectedItem)
			{
				return;
			}

			var fileName = selectedItem.Tag.ToString();

			if (fileName == null)
			{
				return;
			}

			var data = File.ReadAllText(fileName);

			var parts = data.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

			if (fileName.EndsWith("position.csv"))
			{
				currentBacktests.Clear();
				for (int i = 0; i < 1; i++)
				{
					var part = parts[i];
					var backtest = new Backtest();
					var lines = part.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
					for (int j = 0; j < 1000; j++)
					{
						try
						{
							var line = lines[j];
							var a = line.Split(',');
							var trade = new Trade(
								a[0].ToDateTime(),
								a[1].ToDecimal(),
								a[2].ToPositionSide(),
								a[3].ToDecimal(),
								a[4].ToDecimal(),
								a[5].ToDecimal(),
								a[6].ToDecimal(),
								a[7].ToDecimal(),
								a[8].ToDecimal(),
								a[9].ToDecimal(),
								a[10].ToInt(),
								a[11].ToInt()
								);
							backtest.Trades.Add(trade);
						}
						catch
						{
						}
					}
					currentBacktests.Add(backtest);
				}

				BacktestListBox.ItemsSource = currentBacktests;
			}
			else if (fileName.EndsWith("Macro.csv"))
			{

			}
			else
			{
				currentBacktestEvents.Clear();
				for (int i = 0; i < parts.Length; i++)
				{
					var part = parts[i];
					var backtestEvent = new BacktestEvent();
					var lines = part.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
					for (int j = 0; j < lines.Length; j++)
					{
						try
						{
							var line = lines[j];
							var a = line.Split(',');

							if (a[1] != "Long" && a[1] != "Short" && a[1] != "Neutral")
							{
								continue;
							}

							var gridEvent = new GridEvent(
								a[0].ToDateTime(),
								a[1].ToGridType(),
								a[3].Replace("Price:", "").ToDecimal(),
								a[4].Replace("Upper:", "").ToDecimal(),
								a[5].Replace("Lower:", "").ToDecimal(),
								a[8].Replace("Interval:", "").ToDecimal()
								);
							backtestEvent.GridEvents.Add(gridEvent);
						}
						catch (Exception)
						{

							throw;
						}
					}
					currentBacktestEvents.Add(backtestEvent);
				}

				BacktestListBox.ItemsSource = currentBacktestEvents;
			}
		}

		private void BacktestListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			switch (BacktestListBox.SelectedItem)
			{
				case Backtest backtest:
					TradeDataGrid.ItemsSource = backtest.Trades;
					break;

				case BacktestEvent backtestEvent:
					GridEventDataGrid.ItemsSource = backtestEvent.GridEvents;
					break;

				default:
					return;
			}
		}
	}
}