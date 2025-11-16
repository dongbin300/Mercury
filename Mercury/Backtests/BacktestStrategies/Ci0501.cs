using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// long entry
	/// CCI ++ 진입레벨 && 구름대위 && 일목후행스팬 > 종가
	/// long exit
	/// CCI -- 청산레벨 || 구름대안 || 일목후행스팬 < 종가
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci0501(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
		: Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int Cci = 14;
		public int Tenkan = 9;
		public int Kijun = 26;
		public int Senkou = 52;

		public decimal Entry = 0m;
		public decimal Exit = 150m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(Cci);
			chartPack.UseIchimokuCloud(Tenkan, Kijun, Senkou);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			if (c2.Cci < Entry && c1.Cci >= Entry
				&& c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above
				&& c1.IcConversion > c1.IcBase)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			if ((c2.Cci > Exit && c1.Cci <= Exit)
				|| c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside
				|| (c1.IcConversion < c1.IcBase))
			{
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			if (c2.Cci > -Entry && c1.Cci <= -Entry
				&& c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below
				&& c1.IcConversion < c1.IcBase)
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			if ((c2.Cci < -Exit && c1.Cci >= -Exit)
				|| c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside
				|| c1.IcConversion > c1.IcBase)
			{
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}
