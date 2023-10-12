using Mercury;

using MercuryTradingModel.Charts;
using MercuryTradingModel.Enums;
using MercuryTradingModel.Maths;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarinerX.Deals
{
    public class CommasDealManager
    {
        public List<CommasDeal> Deals { get; set; } = new List<CommasDeal>();
        public CommasDeal? LatestDeal => Deals.Count > 0 ? Deals[^1] : null;
        public decimal CurrentPositionQuantity => GetCurrentPositionQuantity();
        public bool IsPositioning => CurrentPositionQuantity > 0.000001m;
        public decimal TotalIncome => GetIncome();
        public MercuryChartInfo ChartInfo { get; set; } = new("", new Quote());
        public decimal Upnl => GetUpnl(ChartInfo);
        public decimal EstimatedTotalIncome => TotalIncome + Upnl;

        public decimal TargetRoe { get; set; }
        public decimal BaseOrderSize { get; set; }
        public decimal SafetyOrderSize { get; set; }
        public int MaxSafetyOrderCount { get; set; }
        public decimal Deviation { get; set; }
        public List<decimal> Deviations { get; private set; } = new();
        public decimal SafetyOrderStepScale { get; set; }
        public decimal SafetyOrderVolumeScale { get; set; }
        public List<decimal> SafetyOrderVolumes { get; private set; } = new();

        public decimal SltpRatio { get; set; }
        private decimal StopLossRoe;
        private decimal TakeProfitRoe;

        public int WinCount { get; set; } = 0;
        public int LoseCount { get; set; } = 0;
        public decimal WinRate => (decimal)WinCount / (WinCount + LoseCount) * 100;

        public CommasDealManager(decimal targetRoe, decimal baseOrderSize, decimal safetyOrderSize, int maxSafetyOrderCount, decimal deviation, decimal stepScale, decimal volumeScale)
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

        public CommasDealManager(decimal sltpRatio, decimal baseOrderSize)
        {
            SltpRatio = sltpRatio;
            BaseOrderSize = baseOrderSize;
        }

        /// <summary>
        /// 매매 확인
        /// </summary>
        /// <param name="info"></param>
        public void Evaluate(MercuryChartInfo info)
        {
            var roe = GetCurrentRoe(info);
            var rsi = info.GetChartElementValue(ChartElementType.rsi);

            // 포지션이 없고 RSI<30 이면 매수
            if (!IsPositioning && rsi > 0 && rsi < 30)
            {
                var price = (info.Quote.High + info.Quote.Low) / 2;
                var quantity = BaseOrderSize / price;
                OpenDeal(info, price, quantity);
            }
            // 포지션이 있고 추가 매수 지점에 도달하면 추가 매수
            else if (IsPositioning && IsAdditionalOpen(info))
            {
                if (LatestDeal == null)
                {
                    return;
                }
                var price = StockUtil.GetPriceByRoe(PositionSide.Long, LatestDeal.BuyAveragePrice, -Deviations[LatestDeal.CurrentSafetyOrderCount]);
                var quantity = SafetyOrderVolumes[LatestDeal.CurrentSafetyOrderCount] / price;
                AdditionalDeal(info, price, quantity);
            }
            // 포지션이 있고 목표 수익률에 도달하면 매도
            else if (IsPositioning && roe >= TargetRoe)
            {
                CloseDeal(info, TargetRoe);
            }
        }

        /// <summary>
        /// 매매 테스트
        /// Target ROE를 따로 지정하지 않고 상황에 따른 손절:익절 비율로 정해짐
        /// </summary>
        /// <param name="info"></param>
        public void Evaluate2(MercuryChartInfo info)
        {
            var roe = GetCurrentRoe(info);
            var macd = info.GetChartElementValue(ChartElementType.macd_hist);
            var ema = info.GetChartElementValue(ChartElementType.ema);
            var ema2 = info.GetChartElementValue(ChartElementType.ema2);

            if (macd == null || ema == null || ema2 == null || ema.Value < 0 || ema2.Value < 0)
            {
                return;
            }

            // 포지션이 없고 MACD +이고 가격이 EMA 위에 있으면 매수
            if (!IsPositioning && macd.Value > 0 && info.Quote.Close > ema.Value && ema.Value > ema2.Value)
            {
                var price = (info.Quote.High + info.Quote.Low) / 2;
                var quantity = BaseOrderSize / price;
                OpenDeal(info, price, quantity);

                // 진입 시의 손절비
                StopLossRoe = StockUtil.Roe(PositionSide.Long, price, ema2.Value);
                // 손절비:익절비 = 1:1.5
                TakeProfitRoe = StopLossRoe * -SltpRatio;
            }
            // 포지션이 있고 가격이 EMA 밑으로 떨어지면 손절
            else if (IsPositioning && info.Quote.Low < ema2.Value)
            {
                CloseDealByStopLoss(info);
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && roe >= TakeProfitRoe)
            {
                CloseDealByTakeProfit(info);
            }
        }

        public void EvaluateRobHoffman(MercuryChartInfo info)
        {
            var q = info.Quote;
            // RH_IRB
            var a = Math.Abs(q.High - q.Low);
            var b = Math.Abs(q.Close - q.Open);
            var c = 0.45m;
            var rv = b < (c * a);
            var x = q.Low + (c * a);
            var y = q.High - (c * a);
            var sl = rv && q.High > y && q.Close < y && q.Open < y; // Long Signal
            var ss = rv && q.Low < x && q.Close > x && q.Open > x; // Short Signal
            var li = sl ? y : ss ? x : (x + y) / 2;
            // RH_OS
            var sls = info.GetChartElementValue(ChartElementType.ma);
            var fpt = info.GetChartElementValue(ChartElementType.ema);
            var t1 = info.GetChartElementValue(ChartElementType.ma2);
            var t2 = info.GetChartElementValue(ChartElementType.ma3);
            var t3 = info.GetChartElementValue(ChartElementType.ema2);
            var roe = GetCurrentRoe(info);

            if (fpt == null)
            {
                return;
            }

            // 포지션이 없으면 Rob Hoffman 매매 시그널에 의해 매수
            if (!IsPositioning && sls > fpt && sl &&
                (t1 > sls && t2 > sls && t3 > sls && t1 > fpt && t2 > fpt && t3 > fpt || t1 < sls && t2 < sls && t3 < sls && t1 < fpt && t2 < fpt && t3 < fpt))
            {
                var price = q.Close;
                var quantity = BaseOrderSize / price;
                OpenDeal(info, price, quantity);

                StopLossRoe = StockUtil.Roe(PositionSide.Long, price, fpt.Value);
                TakeProfitRoe = StopLossRoe * -SltpRatio;
            }
            // 포지션이 있고 가격이 Fast Primary Trend 밑으로 떨어지면 손절
            else if (IsPositioning && q.Low < fpt.Value)
            {
                CloseDealByStopLoss(info);
            }
            // 포지션이 있고 목표 수익률에 도달하면 익절
            else if (IsPositioning && roe >= TakeProfitRoe)
            {
                CloseDealByTakeProfit(info);
            }
        }

        private int everyonesCoinFlag1 = 0;
        public void EvaluateEveryonesCoin(MercuryChartInfo info, MercuryChartInfo preInfo)
        {
            var q = info.Quote;
            var rsi = info.GetChartElementValue(ChartElementType.rsi);
            var preRsi = preInfo.GetChartElementValue(ChartElementType.rsi);
            var lsma10 = info.GetChartElementValue(ChartElementType.lsma);
            var preLsma10 = preInfo.GetChartElementValue(ChartElementType.lsma);
            var lsma30 = info.GetChartElementValue(ChartElementType.lsma2);
            var preLsma30 = preInfo.GetChartElementValue(ChartElementType.lsma2);
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
        public void OpenDeal(MercuryChartInfo info, decimal price, decimal quantity)
        {
            var deal = new CommasDeal();
            deal.OpenTransactions.Add(new CommasOpenTransaction
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
        public void AdditionalDeal(MercuryChartInfo info, decimal price, decimal quantity)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.OpenTransactions.Add(new CommasOpenTransaction
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
        public void CloseDeal(MercuryChartInfo info, decimal roe)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = StockUtil.GetPriceByRoe(PositionSide.Long, LatestDeal.BuyAveragePrice, roe); // 정확히 지정한 ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.BuyQuantity;
        }

        /// <summary>
        /// 전량 익절
        /// </summary>
        /// <param name="info"></param>
        public void CloseDealByTakeProfit(MercuryChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = StockUtil.GetPriceByRoe(PositionSide.Long, LatestDeal.BuyAveragePrice, TakeProfitRoe); // 정확히 목표ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.BuyQuantity;
        }

        /// <summary>
        /// 전량 손절
        /// </summary>
        /// <param name="info"></param>
        public void CloseDealByStopLoss(MercuryChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return;
            }

            LatestDeal.CloseTransaction.Time = info.DateTime;
            LatestDeal.CloseTransaction.Price = StockUtil.GetPriceByRoe(PositionSide.Long, LatestDeal.BuyAveragePrice, StopLossRoe); // 정확히 손절ROE 가격에서 매도
            LatestDeal.CloseTransaction.Quantity = LatestDeal.BuyQuantity;
        }

        public decimal GetUpnl(MercuryChartInfo info)
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

        public decimal GetCurrentRoe(MercuryChartInfo info)
        {
            if (LatestDeal == null || LatestDeal.IsClosed)
            {
                return 0;
            }

            return LatestDeal.GetCurrentRoe(info);
        }

        public decimal GetIncome()
        {
            return Deals.Sum(d => d.Income);
        }

        public bool IsAdditionalOpen(MercuryChartInfo info)
        {
            if (LatestDeal == null)
            {
                return false;
            }

            if (LatestDeal.CurrentSafetyOrderCount == MaxSafetyOrderCount)
            {
                return false;
            }

            return StockUtil.Roe(PositionSide.Long, LatestDeal.BuyAveragePrice, info.Quote.Low) <= -Deviations[LatestDeal.CurrentSafetyOrderCount];
        }
    }
}
