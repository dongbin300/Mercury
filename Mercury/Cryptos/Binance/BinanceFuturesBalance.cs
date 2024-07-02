namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesBalance
	{
		public string AssetName { get; set; }
		public decimal Wallet { get; set; }
		public decimal Available { get; set; }
		public decimal UnrealizedPnl { get; set; }

		public BinanceFuturesBalance(string assetName, decimal wallet, decimal available, decimal unrealizedPnl)
		{
			AssetName = assetName;
			Wallet = wallet;
			Available = available;
			UnrealizedPnl = unrealizedPnl;
		}
	}
}
