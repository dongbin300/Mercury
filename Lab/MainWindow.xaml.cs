using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests.Calculators;
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

			binanceapi

			var b5 = CalculateMaxLeverage(PositionSide.Short, 63860, 56825, 60950, 20);

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

		public decimal CalculateMaxLeverage(PositionSide side, decimal upper, decimal lower, decimal entry, int gridCount)
		{
			decimal seed = 1_000_000;
			decimal lowerLimit = lower * 0.9m;
			decimal upperLimit = upper * 1.1m;
			var tradeAmount = seed / gridCount;
			var gridInterval = (upper - lower) / (gridCount + 1);
			decimal loss = 0;

			if (side == PositionSide.Long)
			{
				for (decimal price = lower; price <= entry; price += gridInterval)
				{
					var coinCount = tradeAmount / price;
					loss += (lowerLimit - price) * coinCount;
				}
			}
			else if (side == PositionSide.Short)
			{
				for (decimal price = upper; price >= entry; price -= gridInterval)
				{
					var coinCount = tradeAmount / price;
					loss += (price - upperLimit) * coinCount;
				}
			}

			if (loss == 0)
			{
				return seed;
			}

			return seed / -loss;
		}
	}
}
