using Binance.Net.Enums;
using Mercury.Extensions;
using System.Windows;

namespace GridMaxLeverageCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		public decimal CalculateMaxLeverage(PositionSide side, decimal upper, decimal lower, decimal entry, int gridCount, decimal riskMargin)
		{
			decimal seed = 1_000_000;
			decimal lowerLimit = lower * (1 - riskMargin);
			decimal upperLimit = upper * (1 + riskMargin);
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

		private void CalculateButton_Click(object sender, RoutedEventArgs e)
		{
			var gridCount = GridCountTextBox.Text.ToInt();
			var lowerPrice = LowerPriceTextBox.Text.ToDecimal();
			var upperPrice = UpperPriceTextBox.Text.ToDecimal();
			var entryPrice = EntryPriceTextBox.Text.ToDecimal();
			var riskMargin = RiskMarginTextBox.Text.ToDecimal();

			var longMaxLeverage = CalculateMaxLeverage(PositionSide.Long, upperPrice, lowerPrice, entryPrice, gridCount, riskMargin);
			var shortMaxLeverage = CalculateMaxLeverage(PositionSide.Short, upperPrice, lowerPrice, entryPrice, gridCount, riskMargin);
			var result = Math.Min(longMaxLeverage, shortMaxLeverage);

			var longLoss = -100m / CalculateMaxLeverage(PositionSide.Long, upperPrice, lowerPrice, entryPrice, gridCount, 0m) * longMaxLeverage;
			var shortLoss = -100m / CalculateMaxLeverage(PositionSide.Short, upperPrice, lowerPrice, entryPrice, gridCount, 0m) * shortMaxLeverage;

			ResultText.Text = $"{result.Round(2)}x   /   L {longLoss.Round(2)}%   /   S {shortLoss.Round(2)}%";
		}
	}
}