using Binance.Net.Objects.Models.Futures;

namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesAccount
	{
		public List<BinanceFuturesAccountAsset> Assets { get; set; }
		public List<BinancePositionInfoUsdt> Positions { get; set; }
		public decimal AvailableBalance { get; set; }
		public decimal TotalMarginBalance { get; set; }
		public decimal TotalUnrealizedProfit { get; set; }
		public decimal TotalWalletBalance { get; set; }

		public BinanceFuturesAccount(List<BinanceFuturesAccountAsset> assets, List<BinancePositionInfoUsdt> positions, decimal availableBalance, decimal totalMarginBalance, decimal totalUnrealizedProfit, decimal totalWalletBalance)
		{
			Assets = assets;
			Positions = positions;
			AvailableBalance = availableBalance;
			TotalMarginBalance = totalMarginBalance;
			TotalUnrealizedProfit = totalUnrealizedProfit;
			TotalWalletBalance = totalWalletBalance;
		}
	}
}
