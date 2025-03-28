using Binance.Net.Enums;

using Mercury;
using Mercury.Apis;
using Mercury.Charts;
using Mercury.Extensions;

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoMonitor;

/// <summary>
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

		foreach (var symbol in symbols)
		{
			var textBlock = new TextBlock()
			{
				Name = $"{symbol}Price",
				Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				FontSize = 13.5
			};

			MainGrid.Children.Add(textBlock);
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
				var textBlock = MainGrid.FindName($"{symbol}Price") as TextBlock;
				if (textBlock == null)
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