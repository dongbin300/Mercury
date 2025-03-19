using System;
using System.ComponentModel;
using System.Windows.Media;

namespace TradeBot.Models
{
	public class BinanceDailyHistory : BinanceHistory
	{
		public DateTime Time { get; set; }
		public decimal Estimated { get; set; }
		public decimal Change { get; set; }
		public decimal ChangePer { get; set; }
		public decimal MaxPer { get; set; }

		[Browsable(false)]
		public string ChangePerString => ChangePer.ToString("P");
		[Browsable(false)]
		public string MaxPerString => MaxPer.ToString("P");
		[Browsable(false)]
		public SolidColorBrush ChangeColor => Change > 0 ? Common.LongColor : Change < 0 ? Common.ShortColor : Common.ForegroundColor;
		[Browsable(false)]
		public SolidColorBrush MaxPerColor => MaxPer == 1 ? Common.LongColor : Common.ForegroundColor;

		public BinanceDailyHistory(DateTime time, decimal estimated, decimal change, decimal changePer, decimal maxPer)
		{
			Time = time;
			Estimated = estimated;
			Change = change;
			ChangePer = changePer;
			MaxPer = maxPer;
		}

		public BinanceDailyHistory(string data)
		{
			var parts = data.Split(',');
			Time = DateTime.Parse(parts[0]);
			Estimated = decimal.Parse(parts[1]);
			Change = decimal.Parse(parts[2]);
			ChangePer = decimal.Parse(parts[3].TrimEnd('%')) / 100;
			MaxPer = decimal.Parse(parts[4].TrimEnd('%')) / 100;
		}

		public override string ToString()
		{
			return $"{Time:yyyy-MM-dd},{Estimated},{Change},{ChangePer:P},{MaxPer:P}";
		}
	}
}
