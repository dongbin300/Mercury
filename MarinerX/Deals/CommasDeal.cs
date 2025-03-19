using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Maths;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarinerX.Deals
{
	public class CommasDeal
    {
        public List<CommasOpenTransaction> OpenTransactions { get; set; } = [];
        public CommasCloseTransaction CloseTransaction { get; set; } = new();
        public bool IsClosed => CloseTransaction.Time >= new DateTime(2000, 1, 1);
        public TimeSpan TakenTime => CloseTransaction.Time - OpenTransactions[0].Time;
        public decimal BuyAveragePrice => OpenTransactions.Count == 0 ? 0 : OpenTransactions.Sum(t => t.Quantity * t.Price) / OpenTransactions.Sum(t => t.Quantity);
        public decimal BuyQuantity => OpenTransactions.Sum(t => t.Quantity);
        public decimal Income => (CloseTransaction.Price - BuyAveragePrice) * CloseTransaction.Quantity - Fee;
        public decimal Roe => Calculator.Roe(PositionSide.Long, BuyAveragePrice, CloseTransaction.Price);
        public int CurrentSafetyOrderCount => OpenTransactions.Count - 1;
        public decimal Fee => (BuyAveragePrice * BuyQuantity + CloseTransaction.Price * CloseTransaction.Quantity) * CustomFee;
        public readonly decimal CustomFee = 0.0005m; // 0.05%

        public override string ToString()
        {
            return $"{TakenTime}, {Income}, {Roe}%";
        }

        public decimal GetCurrentRoe(ChartInfo info)
        {
            return Calculator.Roe(PositionSide.Long, BuyAveragePrice, (info.Quote.Low + info.Quote.High) / 2);
        }
    }
}
