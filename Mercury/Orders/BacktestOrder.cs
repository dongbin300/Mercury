using Mercury.Assets;
using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Interfaces;
using Mercury.Trades;

using Newtonsoft.Json;

namespace Mercury.Orders
{
	public class BacktestOrder(MtmOrderType type, MtmPositionSide side, OrderAmount amount, decimal? price = null) : IOrder
	{
		public MtmOrderType Type { get; set; } = type;
		public MtmPositionSide Side { get; set; } = side;
		public OrderAmount Amount { get; set; } = amount;
		public decimal? Price { get; set; } = price;
		[JsonIgnore]
		public decimal MakerFee => 0.00075m; // use BNB(0.075%)
											 // public decimal MakerFee => 0.0015m; // use Double Fee(0.15%)
		[JsonIgnore]
		public decimal TakerFee => 0.00075m; // use BNB(0.075%)

		public BackTestTradeInfo Run(Asset asset, ChartInfo chart, string tag = "")
		{
			if (Type == MtmOrderType.Market)
			{
				Price = chart.Quote.Close;
			}

			if (Price == null)
			{
				return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Price Error", "", "", "", "", "", "", "", "");
			}

			decimal quantity;
			switch (Amount.OrderType)
			{
				case MtmOrderAmountType.Fixed:
					quantity = decimal.Round(Amount.Value / Price.Value, 2);
					break;

				case MtmOrderAmountType.FixedSymbol:
					quantity = Amount.Value;
					break;

				case MtmOrderAmountType.Seed:
					var transactionAmount = decimal.Round(asset.Seed * Amount.Value, 2);
					quantity = decimal.Round(transactionAmount / Price.Value, 2);
					break;

				case MtmOrderAmountType.Balance:
					var transactionAmount2 = decimal.Round(asset.Balance * Amount.Value, 2);
					quantity = decimal.Round(transactionAmount2 / Price.Value, 2);
					break;

				case MtmOrderAmountType.Asset:
					var estimatedAsset = Price.Value * asset.Position.Value + asset.Balance;
					var transactionAmount3 = decimal.Round(estimatedAsset * Amount.Value, 2);
					quantity = decimal.Round(transactionAmount3 / Price.Value, 2);
					break;

				case MtmOrderAmountType.BalanceSymbol:
					var symbolAmount = asset.Position.Quantity;
					var transactionAmount4 = decimal.Round(symbolAmount * Amount.Value, 2);
					quantity = decimal.Round(transactionAmount4 / Price.Value, 2);
					break;

				default:
					quantity = 0;
					break;
			}

			switch (Side)
			{
				default:
				case MtmPositionSide.None:
					return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Side Error", "", "", "", "", "", "", "", "");

				case MtmPositionSide.Long:
					var buyFee = Buy(asset, Price.Value, quantity);
					var estimatedAsset = Price.Value * asset.Position.Value + asset.Balance;
					return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Buy", $"{Price:#.####}", $"{quantity:#.####}", $"{buyFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", chart.BaseAsset, $"{estimatedAsset:#.##} USDT", tag);

				case MtmPositionSide.Short:
					var sellFee = Sell(asset, Price.Value, quantity);
					var estimatedAsset2 = Price.Value * asset.Position.Value + asset.Balance;
					return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Sell", $"{Price:#.####}", $"{quantity:#.####}", $"{sellFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", chart.BaseAsset, $"{estimatedAsset2:#.##} USDT", tag);

				case MtmPositionSide.Open:
					if (asset.Position.Side == MtmPositionSide.None || asset.Position.Quantity < 0.0001m)
					{
						return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Open Error", "", "", "", "", "", "", "", "");
					}
					else if (asset.Position.Side == MtmPositionSide.Long)
					{
						quantity = asset.Position.Quantity * Amount.Value;
						var openBuyFee = Buy(asset, Price.Value, quantity);
						var openEstimatedAsset = Price.Value * asset.Position.Value + asset.Balance;
						return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Buy", $"{Price:#.####}", $"{quantity:#.####}", $"{openBuyFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", chart.BaseAsset, $"{openEstimatedAsset:#.##} USDT", tag);
					}
					else
					{
						quantity = asset.Position.Quantity * Amount.Value;
						var openSellFee = Sell(asset, Price.Value, quantity);
						var openEstimatedAsset2 = Price.Value * asset.Position.Value + asset.Balance;
						return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Sell", $"{Price:#.####}", $"{quantity:#.####}", $"{openSellFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", chart.BaseAsset, $"{openEstimatedAsset2:#.##} USDT", tag);
					}

				case MtmPositionSide.Close:
					if (asset.Position.Side == MtmPositionSide.None || asset.Position.Quantity < 0.0001m)
					{
						return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Close Error", "", "", "", "", "", "", "", "");
					}
					else if (asset.Position.Side == MtmPositionSide.Short)
					{
						quantity = asset.Position.Quantity * Amount.Value;
						var closeBuyFee = Buy(asset, Price.Value, quantity);
						var closeEstimatedAsset = Price.Value * asset.Position.Value + asset.Balance;
						return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Buy", $"{Price:#.####}", $"{quantity:#.####}", $"{closeBuyFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", chart.BaseAsset, $"{closeEstimatedAsset:#.##} USDT", tag);
					}
					else
					{
						quantity = asset.Position.Quantity * Amount.Value;
						var closeSellFee = Sell(asset, Price.Value, quantity);
						var closeEstimatedAsset2 = Price.Value * asset.Position.Value + asset.Balance;
						return new BackTestTradeInfo(chart.Symbol, chart.DateTime.ToStandardString(), "Sell", $"{Price:#.####}", $"{quantity:#.####}", $"{closeSellFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", chart.BaseAsset, $"{closeEstimatedAsset2:#.##} USDT", tag);
					}
			}
		}

		public BackTestTradeInfo Run(Asset asset, string symbol)
		{
			if (Price == null)
			{
				return new BackTestTradeInfo(symbol, "-", "Side Error", "", "", "", "", "", "", "", "");
			}

			var quantity = Amount.Value;
			switch (Side)
			{
				default:
					return new BackTestTradeInfo(symbol, "-", "Side Error", "", "", "", "", "", "", "", "");

				case MtmPositionSide.Long:
					var buyFee = Buy(asset, Price.Value, quantity);
					var estimatedAsset = Price.Value * asset.Position.Value + asset.Balance;
					return new BackTestTradeInfo(symbol, "-", "Buy", $"{Price:#.####}", $"{quantity:#.####}", $"{buyFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", symbol.Replace("USDT", ""), $"{estimatedAsset:#.##} USDT", "");

				case MtmPositionSide.Short:
					var sellFee = Sell(asset, Price.Value, quantity);
					var estimatedAsset2 = Price.Value * asset.Position.Value + asset.Balance;
					return new BackTestTradeInfo(symbol, "-", "Sell", $"{Price:#.####}", $"{quantity:#.####}", $"{sellFee:#.##}", $"{asset.Balance:#.##}", $"{asset.Position:#.####}", symbol.Replace("USDT", ""), $"{estimatedAsset2:#.##} USDT", "");
			}
		}

		public decimal Buy(Asset asset, decimal price, decimal quantity)
		{
			var transactionAmount = price * quantity;
			var fee = transactionAmount * TakerFee;
			asset.Balance -= transactionAmount + fee;
			asset.Position.Long(quantity, price);

			return fee;
		}

		public decimal Sell(Asset asset, decimal price, decimal quantity)
		{
			var transactionAmount = price * quantity;
			var fee = transactionAmount * TakerFee;
			asset.Balance += transactionAmount;
			asset.Balance -= fee;
			asset.Position.Short(quantity, price);

			return fee;
		}
	}
}
