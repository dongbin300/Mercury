namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesBalance(string assetName, decimal wallet, decimal available, decimal unrealizedPnl)
	{
		public string AssetName { get; set; } = assetName;
		public decimal Wallet { get; set; } = wallet;
		public decimal Available { get; set; } = available;
		public decimal UnrealizedPnl { get; set; } = unrealizedPnl;
	}
}
