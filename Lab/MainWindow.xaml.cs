using Mercury;
using Mercury.Charts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Lab
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public MainWindow()
		{
			InitializeComponent();

			ChartLoader.InitCharts("BTCUSDT", Binance.Net.Enums.KlineInterval.OneMonth);
			var chartPack = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.OneMonth);
			chartPack.UseAtr();

			var atr = chartPack.Charts.Select(x => x.Atr);
		}
	}
}
