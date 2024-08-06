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

			var calculator = new PredictiveRangesRiskCalculator2("", default!, 10, 0m);
			var result1 = calculator.CalculateMaxLeverage(PositionSide.Long, 20000, 10000, 15000, 21, 0);
			var result2 = calculator.CalculateMaxLeverage(PositionSide.Long, 20000, 10000, 15000, 21, 0.05m);
			var result3 = calculator.CalculateMaxLeverage(PositionSide.Long, 20000, 10000, 15000, 21, 0.10m);
			var result4 = calculator.CalculateMaxLeverage(PositionSide.Long, 20000, 10000, 15000, 21, 0.15m);
			var result5 = calculator.CalculateMaxLeverage(PositionSide.Long, 20000, 10000, 15000, 21, 0.20m);
			var result6 = calculator.CalculateMaxLeverage(PositionSide.Long, 20000, 10000, 15000, 21, 0.25m);

			//ChartLoader.InitCharts("BTCUSDT", KlineInterval.OneDay);
			//var chartPack = ChartLoader.GetChartPack("BTCUSDT", KlineInterval.OneDay);
			//chartPack.UsePredictiveRanges();

			//var calculator = new PredictiveRangesRiskCalculator(chartPack.Symbol, [.. chartPack.Charts], 50);
			//var result = calculator.Run(1057);

			//var upper2 = chartPack.Charts.Select(x => x.PredictiveRangesUpper2);
			//var upper = chartPack.Charts.Select(x => x.PredictiveRangesUpper);
			//var average = chartPack.Charts.Select(x => x.PredictiveRangesAverage);
			//var lower = chartPack.Charts.Select(x => x.PredictiveRangesLower);
			//var lower2 = chartPack.Charts.Select(x => x.PredictiveRangesLower2);
		}
	}
}
