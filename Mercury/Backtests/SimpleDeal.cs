using Binance.Net.Enums;

using Mercury.Maths;

namespace Mercury.Backtests
{
    public class SimpleDeal
    {
        public OpenTransaction OpenTransaction { get; set; } = new();
        public CloseTransaction CloseTransaction { get; set; } = new();
        public PositionSide Side { get; set; }
        public bool IsClosed => CloseTransaction.Time >= new DateTime(2000, 1, 1);
        public TimeSpan TakenTime => CloseTransaction.Time - OpenTransaction.Time;
        public decimal Income => Calculator.Pnl(Side, OpenTransaction.Price, CloseTransaction.Price, CloseTransaction.Quantity) - Fee;
        public decimal Roe => Calculator.Roe(Side, OpenTransaction.Price, CloseTransaction.Price);
        public decimal Fee => Calculator.Fee(OpenTransaction.Price, OpenTransaction.Quantity, CloseTransaction.Price, CloseTransaction.Quantity, CustomFee);
        public readonly decimal CustomFee = 0.0005m; // 0.05%

        public override string ToString()
        {
            return $"{TakenTime}, {Income}, {Roe}%";
        }

        /// <summary>
        /// (Min ROE, Max ROE) in one candle.
        /// </summary>
        /// <param name="quote"></param>
        /// <returns></returns>
        public (decimal, decimal) GetCurrentRoe(Quote quote)
        {
            var low = Calculator.Roe(Side, OpenTransaction.Price, quote.Low);
            var high = Calculator.Roe(Side, OpenTransaction.Price, quote.High);

            return low < high ? (low, high) : (high, low);
        }
    }
}
