using Mercury.Charts;
using Mercury.Maths;

namespace Mercury.Backtests
{
    public class DealManager
    {
        public List<Deal> Deals { get; set; } = [];
        public Deal? LatestDeal => Deals.Count > 0 ? Deals[^1] : null;
        public decimal CurrentPositionQuantity => GetCurrentPositionQuantity();
        public bool IsPositioning => CurrentPositionQuantity > 0.000001m;
        public decimal TotalIncome => GetIncome();
        public ChartInfo ChartInfo { get; set; } = new("", new Quote());
        public decimal Upnl => GetUpnl(ChartInfo);
        public decimal EstimatedTotalIncome => TotalIncome + Upnl;

        public decimal TargetRoe { get; set; }
        public decimal BaseOrderSize { get; set; }
        public decimal SafetyOrderSize { get; set; }
        public int MaxSafetyOrderCount { get; set; }
        public decimal Deviation { get; set; }
        public List<decimal> Deviations { get; private set; } = [];
        public decimal SafetyOrderStepScale { get; set; }
        public decimal SafetyOrderVolumeScale { get; set; }
        public List<decimal> SafetyOrderVolumes { get; private set; } = [];

        public decimal SltpRatio { get; set; }
        private decimal StopLossRoe = 0;
        private decimal TakeProfitRoe = 0;

        public int WinCount { get; set; } = 0;
        public int LoseCount { get; set; } = 0;
        public decimal WinRate => (decimal)WinCount / (WinCount + LoseCount) * 100;

        public DealManager(decimal targetRoe, decimal baseOrderSize, decimal safetyOrderSize, int maxSafetyOrderCount, decimal deviation, decimal stepScale, decimal volumeScale)
        {
            TargetRoe = targetRoe;
            BaseOrderSize = baseOrderSize;
            SafetyOrderSize = safetyOrderSize;
            MaxSafetyOrderCount = maxSafetyOrderCount;
            Deviation = deviation;
            SafetyOrderStepScale = stepScale;
            SafetyOrderVolumeScale = volumeScale;
            for (int i = 0; i < maxSafetyOrderCount; i++)
            {
                if (i == 0)
                {
                    Deviations.Add(Deviation);
                    SafetyOrderVolumes.Add(SafetyOrderSize);
                }
                else
                {
                    Deviations.Add(Deviations[i - 1] + Deviation * (decimal)Math.Pow((double)SafetyOrderStepScale, i));
                    SafetyOrderVolumes.Add(SafetyOrderVolumes[i - 1] + SafetyOrderSize * (decimal)Math.Pow((double)SafetyOrderVolumeScale, i));
                }
            }
        }

        public DealManager(decimal sltpRatio, decimal baseOrderSize)
        {
            SltpRatio = sltpRatio;
            BaseOrderSize = baseOrderSize;
        }

        private int everyonesCoinFlag1 = 0;
        public void EvaluateEveryonesCoin(ChartInfo info, ChartInfo preInfo)
        {
            var q = info.Quote;
            var rsi = info.Rsi1;
            var preRsi = preInfo.Rsi1;
            var lsma10 = info.Lsma1;
            var preLsma10 = preInfo.Lsma1;
            var lsma30 = info.Lsma2;
            var preLsma30 = preInfo.Lsma2;
            var roe = GetCurrentRoe(info);

            everyonesCoinFlag1--;
            // RSI 40 골든 크로스
            if (preRsi < 40 && rsi >= 40)
            {
                everyonesCoinFlag1 = 3;
            }

            // 포지션이 없고 RSI 40라인을 골든 크로스 이후, 3봉 이내에 LSMA 10이 30을 골든 크로스하면 매수
            if (!IsPositioning && everyonesCoinFlag1 >= 0 && preLsma10 < preLsma30 && lsma10 > lsma30)
            {
                var price = (q.High + q.Low) / 2;
                var quantity = BaseOrderSize / price;
                OpenDeal(info, price, quantity);
            }
            // 포지션이 있고 목표 수익률의 절반만큼 손실일 경우 손절
            else if (IsPositioning && roe <= TargetRoe / -2)
            {
                CloseDeal(info, TargetRoe / -2);
                LoseCount++;
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && roe >= TargetRoe)
            {
                CloseDeal(info, TargetRoe);
                WinCount++;
            }
        }

        /// <summary>
        /// 포지션 진입
        /// </summary>
        /// <param name="info"></param>
        /// <param name="price"></param>
        /// <param name="quantity"></param>
        public void OpenDeal(ChartInfo info, decimal price, decimal quantity)
        {
            var deal = new Deal();
            deal.OpenTransactions.Add(new OpenTransaction
            {
                Time = info.DateTime,
                Price = price,
                Quantity = quantity
            });
            Deals.Add(deal);
        }

        /// <summary>
        /// 추가 포지셔닝
        /// </summary>
        /// <param name="info"></param>
        public void AdditionalDeal(ChartInfo info, decimal price, decimal quantity)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.OpenTransactions.Add(new OpenTransaction
            {
                Time = info.DateTime,
                Price = price,
                Quantity = quantity
            });
        }

        /// <summary>
        /// 전량 정리
        /// </summary>
        /// <param name="info"></param>
        /// <param name="roe"></param>
        public void CloseDeal(ChartInfo info, decimal roe)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = Calculator.TargetPrice(Binance.Net.Enums.PositionSide.Long, LatestDeal.BuyAveragePrice, roe); // 정확히 지정한 ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.BuyQuantity;
        }

        /// <summary>
        /// 전량 익절
        /// </summary>
        /// <param name="info"></param>
        public void CloseDealByTakeProfit(ChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = Calculator.TargetPrice(Binance.Net.Enums.PositionSide.Long, LatestDeal.BuyAveragePrice, TakeProfitRoe); // 정확히 목표ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.BuyQuantity;
        }

        /// <summary>
        /// 전량 손절
        /// </summary>
        /// <param name="info"></param>
        public void CloseDealByStopLoss(ChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = Calculator.TargetPrice(Binance.Net.Enums.PositionSide.Long, LatestDeal.BuyAveragePrice, StopLossRoe); // 정확히 손절ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.BuyQuantity;
        }

        public decimal GetUpnl(ChartInfo info)
        {
            var inProgressDeals = Deals.Where(d => !d.IsClosed);
            if (inProgressDeals == null)
            {
                return 0;
            }

            return inProgressDeals.Sum(d => (info.Quote.Close - d.BuyAveragePrice) * d.BuyQuantity);
        }

        public decimal GetCurrentPositionQuantity()
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return 0;
            }

            return LatestDeal.BuyQuantity;
        }

        public decimal GetCurrentRoe(ChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return 0;
            }

            return LatestDeal.GetCurrentRoe(info.Quote);
        }

        public decimal GetIncome()
        {
            return Deals.Sum(d => d.Income);
        }

        public bool IsAdditionalOpen(ChartInfo info)
        {
            if (LatestDeal == null)
            {
                return false;
            }

            if (LatestDeal.CurrentSafetyOrderCount == MaxSafetyOrderCount)
            {
                return false;
            }

            return Calculator.Roe(Binance.Net.Enums.PositionSide.Long, LatestDeal.BuyAveragePrice, info.Quote.Low) <= -Deviations[LatestDeal.CurrentSafetyOrderCount];
        }
    }
}
