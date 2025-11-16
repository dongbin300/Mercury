using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci02(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 20;
		public int IchimokuTenkan = 12;
		public int IchimokuKijun = 20;
		public int IchimokuSenkou = 60;

		public decimal EntryLevel = 100m;
		public decimal ExitLevel = 0m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			bool cciStrongUp = c3.Cci < EntryLevel && c1.Cci > EntryLevel && c1.Cci > c2.Cci;
			bool tenkanCrossUp = c2.IcConversion <= c2.IcBase && c1.IcConversion > c1.IcBase;
			bool cloudBullish = c1.IcLeadingSpan1 > c1.IcLeadingSpan2;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			if (cciStrongUp && tenkanCrossUp && cloudBullish && priceAboveCloud)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnDown = c2.Cci > ExitLevel && c1.Cci < c2.Cci;
			bool tenkanCrossDown = c2.IcConversion >= c2.IcBase && c1.IcConversion < c1.IcBase;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnDown || tenkanCrossDown || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			bool cciStrongDown = c3.Cci > -EntryLevel && c1.Cci < -EntryLevel && c1.Cci < c2.Cci;
			bool tenkanCrossDown = c2.IcConversion >= c2.IcBase && c1.IcConversion < c1.IcBase;
			bool cloudBearish = c1.IcLeadingSpan1 < c1.IcLeadingSpan2;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			if (cciStrongDown && tenkanCrossDown && cloudBearish && priceBelowCloud)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnUp = c2.Cci < -ExitLevel && c1.Cci > c2.Cci;
			bool tenkanCrossUp = c2.IcConversion <= c2.IcBase && c1.IcConversion > c1.IcBase;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnUp || tenkanCrossUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

}
