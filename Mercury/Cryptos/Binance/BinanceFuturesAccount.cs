using Binance.Net.Objects.Models.Futures;

namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesAccount(List<BinanceFuturesAccountInfoAsset> assets, List<BinanceFuturesAccountInfoPosition> positions, decimal availableBalance, decimal totalMarginBalance, decimal totalUnrealizedProfit, decimal totalWalletBalance)
	{
		public List<BinanceFuturesAccountInfoAsset> Assets { get; set; } = assets;
		public List<BinanceFuturesAccountInfoPosition> Positions { get; set; } = positions;
		public decimal AvailableBalance { get; set; } = availableBalance;
		public decimal TotalMarginBalance { get; set; } = totalMarginBalance;
		public decimal TotalUnrealizedProfit { get; set; } = totalUnrealizedProfit;
		public decimal TotalWalletBalance { get; set; } = totalWalletBalance;
	}
}
