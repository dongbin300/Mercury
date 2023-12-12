using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Maths;

namespace Mercury.Backtests
{
    public class SimpleDealManager
    {
        public List<SimpleDeal> Deals { get; set; } = new();
        public SimpleDeal? LatestDeal => Deals.Count > 0 ? Deals[^1] : null;
        public decimal CurrentPositionQuantity => GetCurrentPositionQuantity();
        public bool IsPositioning => CurrentPositionQuantity > 0.000001m;
        public decimal TotalIncome => GetIncome();
        public ChartInfo ChartInfo { get; set; } = new("", new Quote());
        public decimal Upnl => GetUpnl(ChartInfo);
        public decimal EstimatedTotalIncome => TotalIncome + Upnl;

        public decimal? TargetRoe { get; set; }
        public decimal BaseOrderSize { get; set; }

        public decimal? SltpRatio { get; set; }
        private decimal StopLossRoe;
        private decimal TakeProfitRoe;

        public int WinCount { get; set; } = 0;
        public int LoseCount { get; set; } = 0;
        public decimal WinRate => (decimal)WinCount / (WinCount + LoseCount) * 100;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int BacktestDays => (int)(EndDate - StartDate).TotalDays;
        public decimal IncomePerDay => TotalIncome / BacktestDays;

        public SimpleDealManager(DateTime startDate, DateTime endDate, decimal baseOrderSize, decimal? targetRoe = null, decimal? sltpRatio = null)
        {
            BaseOrderSize = baseOrderSize;
            TargetRoe = targetRoe;
            SltpRatio = sltpRatio;
            StartDate = startDate;
            EndDate = endDate;
        }

        #region Everyones Coin
        private int everyonesCoinFlag1 = 0;
        public void EvaluateEveryonesCoinLong(ChartInfo info, ChartInfo preInfo)
        {
            if (TargetRoe == null)
            {
                return;
            }

            var q = info.Quote;
            var rsi = info.Rsi1;
            var preRsi = preInfo.Rsi1;
            var lsma10 = info.Lsma1;
            var preLsma10 = preInfo.Lsma1;
            var lsma30 = info.Lsma2;
            var preLsma30 = preInfo.Lsma2;
            var side = PositionSide.Long;
            (var minRoe, var maxRoe) = GetCurrentRoe(info);

            everyonesCoinFlag1--;
            // RSI 40 골든 크로스
            if (preRsi < 40 && rsi > 40)
            {
                everyonesCoinFlag1 = 3;
            }

            // 포지션이 없고 RSI 40라인을 골든 크로스 이후, 3봉 이내에 LSMA 10이 30을 골든 크로스하면 매수
            if (!IsPositioning && everyonesCoinFlag1 >= 0 && preLsma10 < preLsma30 && lsma10 > lsma30)
            {
                var price = q.Open;
                var quantity = BaseOrderSize / price;
                OpenDeal(info, price, quantity, side);
            }
            // 포지션이 있고 목표 수익률의 절반만큼 손실일 경우 손절
            else if (IsPositioning && minRoe <= TargetRoe / -2)
            {
                CloseDeal(info, TargetRoe.Value / -2, side);
                LoseCount++;
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && maxRoe >= TargetRoe)
            {
                CloseDeal(info, TargetRoe.Value, side);
                WinCount++;
            }
        }

        public void EvaluateEveryonesCoinShort(ChartInfo info, ChartInfo preInfo)
        {
            if (TargetRoe == null)
            {
                return;
            }

            var q = info.Quote;
            var rsi = info.Rsi1;
            var preRsi = preInfo.Rsi1;
            var lsma10 = info.Lsma1;
            var preLsma10 = preInfo.Lsma1;
            var lsma30 = info.Lsma2;
            var preLsma30 = preInfo.Lsma2;
            var side = PositionSide.Short;
            (var minRoe, var maxRoe) = GetCurrentRoe(info);

            everyonesCoinFlag1--;
            // RSI 60 데드 크로스
            if (preRsi > 60 && rsi < 60)
            {
                everyonesCoinFlag1 = 3;
            }

            // 포지션이 없고 RSI 60라인을 데드 크로스 이후, 3봉 이내에 LSMA 10이 30을 데드 크로스하면 매수
            if (!IsPositioning && everyonesCoinFlag1 >= 0 && preLsma10 > preLsma30 && lsma10 < lsma30)
            {
                var price = q.Open;
                var quantity = BaseOrderSize / price;
                OpenDeal(info, price, quantity, side);
            }
            // 포지션이 있고 목표 수익률의 절반만큼 손실일 경우 손절
            else if (IsPositioning && minRoe <= TargetRoe / -2)
            {
                CloseDeal(info, TargetRoe.Value / -2, side);
                LoseCount++;
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && maxRoe >= TargetRoe)
            {
                CloseDeal(info, TargetRoe.Value, side);
                WinCount++;
            }
        }
        #endregion

        #region Stefano

        public void EvaluateStefanoLong(ChartInfo info, ChartInfo preInfo, decimal slSafeThreshold, decimal slSafeRate)
        {
            if (SltpRatio == null)
            {
                return;
            }

            var q = info.Quote;
            var ema12 = info.Ema1;
            var ema26 = info.Ema2;
            var preEma12 = preInfo.Ema1;
            var preEma26 = preInfo.Ema2;
            var side = PositionSide.Long;
            (var minRoe, var maxRoe) = GetCurrentRoe(info);

            // 포지션이 없고 EMA 12가 26을 골든 크로스하면 진입
            if (!IsPositioning && preEma12 < preEma26 && ema12 > ema26)
            {
                var price = q.Open;
                var quantity = BaseOrderSize / price;
                // 손절가: 진입가와 EMA 26 차이가 임계값 이상일 경우 EMA 26, 임계값 미만일 경우 EMA 26보다 조금 아래
                var stopLossPrice =
                    Calculator.Roe(PositionSide.Short, price, (decimal)ema26) >= slSafeThreshold ?
                    (decimal)ema26 :
                    Calculator.TargetPrice(PositionSide.Short, (decimal)ema26, slSafeRate);
                StopLossRoe = Calculator.Roe(PositionSide.Long, price, stopLossPrice);
                TakeProfitRoe = StopLossRoe * -SltpRatio.Value;

                OpenDeal(info, price, quantity, side);
            }
            // 포지션이 있고 손절가에 도달할 경우 손절
            else if (IsPositioning && minRoe <= StopLossRoe)
            {
                CloseDealByStopLoss(info, side);
                LoseCount++;
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && maxRoe >= TakeProfitRoe)
            {
                CloseDealByTakeProfit(info, side);
                WinCount++;
            }
        }

        public void EvaluateStefanoShort(ChartInfo info, ChartInfo preInfo, decimal slSafeThreshold, decimal slSafeRate)
        {
            if (SltpRatio == null)
            {
                return;
            }

            var q = info.Quote;
            var ema12 = info.Ema1;
            var ema26 = info.Ema2;
            var preEma12 = preInfo.Ema1;
            var preEma26 = preInfo.Ema2;
            var side = PositionSide.Short;
            (var minRoe, var maxRoe) = GetCurrentRoe(info);

            // 포지션이 없고 EMA 12가 26을 데드 크로스하면 진입
            if (!IsPositioning && preEma12 > preEma26 && ema12 < ema26)
            {
                var price = q.Open;
                var quantity = BaseOrderSize / price;
                // 손절가: 진입가와 EMA 26 차이가 임계값 이상일 경우 EMA 26, 임계값 미만일 경우 EMA 26보다 조금 위
                var stopLossPrice =
                    Calculator.Roe(PositionSide.Long, price, (decimal)ema26) >= slSafeThreshold ?
                    (decimal)ema26 :
                    Calculator.TargetPrice(PositionSide.Long, (decimal)ema26, slSafeRate);
                StopLossRoe = Calculator.Roe(PositionSide.Short, price, stopLossPrice);
                TakeProfitRoe = StopLossRoe * -SltpRatio.Value;

                OpenDeal(info, price, quantity, side);
            }
            // 포지션이 있고 손절가에 도달할 경우 손절
            else if (IsPositioning && minRoe <= StopLossRoe)
            {
                CloseDealByStopLoss(info, side);
                LoseCount++;
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && maxRoe >= TakeProfitRoe)
            {
                CloseDealByTakeProfit(info, side);
                WinCount++;
            }
        }
        #endregion

        /// <summary>
        /// 포지션 진입
        /// </summary>
        /// <param name="info"></param>
        /// <param name="price"></param>
        /// <param name="quantity"></param>
        public void OpenDeal(ChartInfo info, decimal price, decimal quantity, PositionSide side)
        {
            var deal = new SimpleDeal
            {
                Side = side,
                OpenTransaction = new OpenTransaction
                {
                    Time = info.DateTime,
                    Price = price,
                    Quantity = quantity
                }
            };
            Deals.Add(deal);
        }

        /// <summary>
        /// 전량 정리
        /// </summary>
        /// <param name="info"></param>
        /// <param name="roe"></param>
        public void CloseDeal(ChartInfo info, decimal roe, PositionSide side)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = Calculator.TargetPrice(side, LatestDeal.OpenTransaction.Price, roe); // 정확히 지정한 ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.OpenTransaction.Quantity;
        }

        /// <summary>
        /// 전량 익절
        /// </summary>
        /// <param name="info"></param>
        public void CloseDealByTakeProfit(ChartInfo info, PositionSide side)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = Calculator.TargetPrice(side, LatestDeal.OpenTransaction.Price, TakeProfitRoe); // 정확히 목표ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.OpenTransaction.Quantity;
        }

        /// <summary>
        /// 전량 손절
        /// </summary>
        /// <param name="info"></param>
        public void CloseDealByStopLoss(ChartInfo info, PositionSide side)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = Calculator.TargetPrice(side, LatestDeal.OpenTransaction.Price, StopLossRoe); // 정확히 손절ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.OpenTransaction.Quantity;
        }

        public decimal GetUpnl(ChartInfo info)
        {
            var inProgressDeals = Deals.Where(d => !d.IsClosed);
            if (inProgressDeals == null)
            {
                return 0;
            }

            return inProgressDeals.Sum(d => (info.Quote.Close - d.OpenTransaction.Price) * d.OpenTransaction.Quantity);
        }

        public decimal GetCurrentPositionQuantity()
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return 0;
            }

            return LatestDeal.OpenTransaction.Price;
        }

        public (decimal, decimal) GetCurrentRoe(ChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return (0, 0);
            }

            return LatestDeal.GetCurrentRoe(info.Quote);
        }

        public decimal GetIncome()
        {
            return Deals.Sum(d => d.Income);
        }
    }
}
