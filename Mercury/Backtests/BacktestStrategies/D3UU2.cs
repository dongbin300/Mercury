using Binance.Net.Enums;

using Mercury.Backtests.BacktestInterfaces;
using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class D3UU2(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals), IUseBlacklist
	{
		public List<BlacklistPosition> BlacklistPositions { get; set; } = [];
		public decimal BlacklistLossThresholdPercent { get; set; }
		public int BlacklistBanHour { get; set; }

		public decimal CloseBodyLengthMin { get; set; }

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{

		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var time = c0.DateTime;

			if (
				c1.CandlestickType == CandlestickType.Bearish
				&& c2.CandlestickType == CandlestickType.Bearish
				&& c3.CandlestickType == CandlestickType.Bearish
				&& c1.BodyLength > 0.05m
				&& c2.BodyLength > 0.05m
				&& c3.BodyLength > 0.05m
				&& !((IUseBlacklist)this).IsBannedPosition(symbol, PositionSide.Long, time)
				)
			{
				EntryPosition(PositionSide.Long, c0, c1.Quote.Close);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var time = c0.DateTime;

			if (
				c1.CandlestickType == CandlestickType.Bullish
				&& c2.CandlestickType == CandlestickType.Bullish
				&& c1.BodyLength + c2.BodyLength >= CloseBodyLengthMin
				)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				if (((IUseBlacklist)this).IsPostBlacklist(PositionSide.Long, longPosition.EntryPrice, c1.Quote.Close)) // 손실이 임계치를 벗어나면 블랙리스트 등록
				{
					var blacklistPosition = new BlacklistPosition(symbol, PositionSide.Long, time, time.AddHours(BlacklistBanHour));
					((IUseBlacklist)this).AddBlacklist(blacklistPosition);
				}
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var time = c0.DateTime;

			if (
				c1.CandlestickType == CandlestickType.Bullish
				&& c2.CandlestickType == CandlestickType.Bullish
				&& c3.CandlestickType == CandlestickType.Bullish
				&& c1.BodyLength > 0.05m
				&& c2.BodyLength > 0.05m
				&& c3.BodyLength > 0.05m
				&& !((IUseBlacklist)this).IsBannedPosition(symbol, PositionSide.Short, time)
				)
			{
				EntryPosition(PositionSide.Short, c0, c1.Quote.Close);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var time = c0.DateTime;

			if (
				c1.CandlestickType == CandlestickType.Bearish
				&& c2.CandlestickType == CandlestickType.Bearish
				&& c1.BodyLength + c2.BodyLength >= CloseBodyLengthMin
				)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				if (((IUseBlacklist)this).IsPostBlacklist(PositionSide.Short, shortPosition.EntryPrice, c1.Quote.Close)) // 손실이 임계치를 벗어나면 블랙리스트 등록
				{
					var blacklistPosition = new BlacklistPosition(symbol, PositionSide.Short, time, time.AddHours(BlacklistBanHour));
					((IUseBlacklist)this).AddBlacklist(blacklistPosition);
				}
			}
		}
	}
}
