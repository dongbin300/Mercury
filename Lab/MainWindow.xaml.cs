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

			ChartLoader.InitCharts("BTCUSDT", Binance.Net.Enums.KlineInterval.OneDay);
			var chartPack = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.OneDay);
			chartPack.UsePredictiveRanges();

			var upper2 = chartPack.Charts.Select(x => x.PredictiveRangesUpper2);
			var upper = chartPack.Charts.Select(x => x.PredictiveRangesUpper);
			var average = chartPack.Charts.Select(x => x.PredictiveRangesAverage);
			var lower = chartPack.Charts.Select(x => x.PredictiveRangesLower);
			var lower2 = chartPack.Charts.Select(x => x.PredictiveRangesLower2);
		}
	}
}
