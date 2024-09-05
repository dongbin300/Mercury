using Binance.Net.Enums;

namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesPosition(string symbol, FuturesMarginType marginType, int leverage, PositionSide positionSide, decimal quantity, decimal entryPrice, decimal markPrice, decimal unrealizedPnl, decimal liquidationPrice)
	{
		public string Symbol { get; set; } = symbol;
		public FuturesMarginType MarginType { get; set; } = marginType;
		public int Leverage { get; set; } = leverage;
		public PositionSide PositionSide { get; set; } = positionSide;
		public decimal Quantity { get; set; } = quantity;
		public decimal EntryPrice { get; set; } = entryPrice;
		public decimal MarkPrice { get; set; } = markPrice;
		public decimal UnrealizedPnl { get; set; } = unrealizedPnl;
		public decimal LiquidationPrice { get; set; } = liquidationPrice;
	}
}
