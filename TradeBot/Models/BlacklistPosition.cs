using Binance.Net.Enums;
using Mercury.Extensions;
using System;
using System.Windows.Media;

namespace TradeBot.Models
{
    public class BlacklistPosition
	{
		public string Symbol { get; set; }
		public PositionSide Side { get; set; }
		public SolidColorBrush PositionSideColor => Side == PositionSide.Long ? Common.LongColor : Common.ShortColor;

		/// <summary>
		/// 발동 시각
		/// </summary>
		public DateTime TriggerTime { get; set; }

		/// <summary>
		/// 해제 시각
		/// </summary>
		public DateTime ReleaseTime { get; set; }
		public string ReleaseTimeString => ReleaseTime.ToString("yyyy-MM-dd HH:mm:ss");

		public bool IsBanned(DateTime time) => time >= TriggerTime && time < ReleaseTime;

		public BlacklistPosition(string symbol, PositionSide side, DateTime triggerTime, DateTime releaseTime)
		{
			Symbol = symbol;
			Side = side;
			TriggerTime = triggerTime;
			ReleaseTime = releaseTime;
		}

		public BlacklistPosition(string line)
		{
			var parts = line.Split(',');
			Symbol = parts[0];
			Side = parts[1].ToPositionSide();
			TriggerTime = parts[2].ToDateTime();
			ReleaseTime = parts[3].ToDateTime();
		}

		public override string ToString()
		{
			return $"{Symbol},{Side},{TriggerTime:yyyy-MM-dd HH:mm:ss},{ReleaseTime:yyyy-MM-dd HH:mm:ss}";
		}
	}
}
