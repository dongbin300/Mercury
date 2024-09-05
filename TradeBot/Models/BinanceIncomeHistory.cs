using Binance.Net.Enums;

using System;

namespace TradeBot.Models
{
	public class BinanceIncomeHistory : BinanceHistory
	{
		public DateTime Time { get; set; }
		public string TransactionId { get; set; }
		public string? Symbol { get; set; }
		public IncomeType? IncomeType { get; set; }
		public decimal Income { get; set; }
		public string? Asset { get; set; }
		public string? Info { get; set; }

		public BinanceIncomeHistory(DateTime time, string transactionId, string? symbol, IncomeType? incomeType, decimal income, string? asset, string? info)
		{
			Time = time;
			TransactionId = transactionId;
			Symbol = symbol;
			IncomeType = incomeType;
			Income = income;
			Asset = asset;
			Info = info;
		}

		public BinanceIncomeHistory(string data)
		{
			var parts = data.Split(',');
			Time = DateTime.Parse(parts[0]);
			TransactionId = parts[1];
			Symbol = parts[2];
			IncomeType = (IncomeType)Enum.Parse(typeof(IncomeType), parts[3]);
			Income = decimal.Parse(parts[4]);
			Asset = parts[5];
			Info = parts[6];
		}

		public override string ToString()
		{
			return $"{Time:yyyy-MM-dd HH:mm:ss},{TransactionId},{Symbol},{(IncomeType == null ? string.Empty : IncomeType.Value)},{Income},{Asset},{Info}";
		}
	}
}
