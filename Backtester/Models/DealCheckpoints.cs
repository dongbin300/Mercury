using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Maths;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Backtester.Models
{
    public class DealCheckpoints
    {
        public string Symbol { get; set; }
        public DateTime EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public PositionSide Side { get; set; }
        public IList<Checkpoint> Histories { get; set; } = new List<Checkpoint>();
        public IList<decimal> Roes => Histories.Select(x => Calculator.Roe(Side, EntryPrice, x.Price)).ToList();
        public Checkpoint? HighCheckpoint => Histories.Where(x => x.Direction.Equals(Side == PositionSide.Long ? CheckpointDirection.Profit : CheckpointDirection.Loss)).OrderByDescending(x => x.Time).FirstOrDefault();
        public Checkpoint? LowCheckpoint => Histories.Where(x => x.Direction.Equals(Side == PositionSide.Long ? CheckpointDirection.Loss : CheckpointDirection.Profit)).OrderByDescending(x => x.Time).FirstOrDefault();
        public int Life { get; set; }

        public DealCheckpoints(string symbol, DateTime entryTime, decimal entryPrice, PositionSide side)
        {
            Symbol = symbol;
            EntryTime = entryTime;
            EntryPrice = entryPrice;
            Side = side;
            Life = 0;
        }

        public void EvaluateCheckpoint(ChartInfo info)
        {
            var time = info.DateTime;
            var high = info.Quote.High;
            var low = info.Quote.Low;

            if (HighCheckpoint == null)
            {
                if (high > EntryPrice)
                {
                    AddCheckpoint(time, Side == PositionSide.Long ? CheckpointDirection.Profit : CheckpointDirection.Loss, high);
                }
            }
            else
            {
                if (high > HighCheckpoint.Price)
                {
                    AddCheckpoint(time, Side == PositionSide.Long ? CheckpointDirection.Profit : CheckpointDirection.Loss, high);
                }
            }

            if (LowCheckpoint == null)
            {
                if (low < EntryPrice)
                {
                    AddCheckpoint(time, Side == PositionSide.Long ? CheckpointDirection.Loss : CheckpointDirection.Profit, low);
                }
            }
            else
            {
                if (low < LowCheckpoint.Price)
                {
                    AddCheckpoint(time, Side == PositionSide.Long ? CheckpointDirection.Loss : CheckpointDirection.Profit, low);
                }
            }
        }

        public void AddCheckpoint(DateTime time, CheckpointDirection direction, decimal price)
        {
            Histories.Add(new Checkpoint(time, direction, price));
        }

        public void ArrangeHistories()
        {
            var newHistories = new List<Checkpoint>
            {
                Histories[0]
            };

            for (int i = 1; i < Histories.Count; i++)
            {
                var checkpoint = Histories[i];
                var prevCheckpoint = Histories[i - 1];

                if (checkpoint.Direction != prevCheckpoint.Direction || i == Histories.Count - 1)
                {
                    newHistories.Add(checkpoint);
                }
            }

            Histories = newHistories;
        }

        /// <summary>
        /// 목표 수익에 대해 익절인지 손절인지 판단 (손절비 1:1)
        /// 이겼을 경우 1, 졌을 경우 -1, 결과가 안났을 경우 0 반환
        /// </summary>
        /// <param name="targetRoe"></param>
        /// <returns></returns>
        public int EvaluateDealResult(decimal targetRoe)
        {
            foreach (var roe in Roes.Where(roe => Math.Abs(roe) >= targetRoe))
            {
                return roe > 0 ? 1 : -1;
            }

            return 0;
        }

        public override string ToString()
        {
            return string.Join(", ", Histories.Select(x=> Calculator.Roe(Side, EntryPrice, x.Price) + "%"));
        }
    }
}
