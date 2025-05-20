using Mercury;
using Mercury.Apis;
using Mercury.Charts;
using Mercury.Extensions;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoMonitor;

/// <summary>+
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
	string[] symbols;
	public MainWindow()
	{
		InitializeComponent();

		symbols = File.ReadAllLines(MercuryPath.Base.Down("mcdata.txt"));

		MainGrid.RowDefinitions.Clear();
		for (int i = 0; i < symbols.Length; i++)
		{
			MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

			var symbol = symbols[i];
			var textBlock = new TextBlock()
			{
				Name = $"{symbol}Price",
				Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
				Background = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 13.5
			};

			Grid.SetRow(textBlock, i);

			MainGrid.Children.Add(textBlock);
			MainGrid.RegisterName(textBlock.Name, textBlock);
		}

		BinanceSocketApi.Init();
		BinanceSocketApi.GetAllMarketMiniTickersAsync();

		dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
		dispatcherTimer.Tick += OnDispatcherTimerTick;
		dispatcherTimer.Start();
	}

	private void OnDispatcherTimerTick(object? sender, EventArgs e)
	{
		try
		{
			foreach (var symbol in symbols)
			{
				var data = QuoteFactory.CurrentPrices.Find(x => x.Symbol.Equals(symbol));
				if (data == null)
				{
					return;
				}
				if (MainGrid.FindName($"{symbol}Price") is not TextBlock textBlock)
				{
					return;
				}
				textBlock.Text = data.Price.ToString();
			}
		}
		catch (Exception ex)
		{
		}

	}
}