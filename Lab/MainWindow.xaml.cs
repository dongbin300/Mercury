using Binance.Net.Enums;

using Mercury.Backtests.Calculators;
using Mercury.Charts;

using System.Collections.Generic;
using System.Linq;
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

			var calculator = new PredictiveRangesRiskCalculator(chartPack.Symbol, [.. chartPack.Charts], 50);
			var result = calculator.Run(1057);

			var upper2 = chartPack.Charts.Select(x => x.PredictiveRangesUpper2);
			var upper = chartPack.Charts.Select(x => x.PredictiveRangesUpper);
			var average = chartPack.Charts.Select(x => x.PredictiveRangesAverage);
			var lower = chartPack.Charts.Select(x => x.PredictiveRangesLower);
			var lower2 = chartPack.Charts.Select(x => x.PredictiveRangesLower2);
		}
	}
}
