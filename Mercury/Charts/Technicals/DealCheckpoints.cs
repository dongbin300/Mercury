using Binance.Net.Enums;

using Mercury.Maths;

namespace Mercury.Charts.Technicals
{
	public class DealCheckpoints(string symbol, DateTime entryTime, decimal entryPrice, PositionSide side)
	{
		public string Symbol { get; set; } = symbol;
		public DateTime EntryTime { get; set; } = entryTime;
		public decimal EntryPrice { get; set; } = entryPrice;
		public PositionSide Side { get; set; } = side;
		public IList<DealCheckpoint> Histories { get; set; } = [];
		public IList<decimal> Roes => [.. Histories.Select(x => Calculator.Roe(Side, EntryPrice, x.Price))];
		public DealCheckpoint? HighCheckpoint => Histories.Where(x => x.Direction.Equals(Side == PositionSide.Long ? CheckpointDirection.Profit : CheckpointDirection.Loss)).OrderByDescending(x => x.Time).FirstOrDefault();
		public DealCheckpoint? LowCheckpoint => Histories.Where(x => x.Direction.Equals(Side == PositionSide.Long ? CheckpointDirection.Loss : CheckpointDirection.Profit)).OrderByDescending(x => x.Time).FirstOrDefault();
		public int Life { get; set; } = 0;

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
			Histories.Add(new DealCheckpoint(time, direction, price));
		}

		public void ArrangeHistories()
		{
			var newHistories = new List<DealCheckpoint>
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

		/// <summary>
		/// ROE Range의 평균을 구한다.
		/// -Roe 다음에 오는 +Roe와의 비율의 평균을 구한다.
		/// EX) Roes = 0.1% -0.2% 0.3% -0.4% 0.5% -1.0% 0.8% 5.5%
		/// 1:1.5, 1:1.25, 1:0.8
		/// 여기서 1.5, 1.25, 0.8의 기하평균 값이 ROE Range의 평균이다.
		/// (1.5*1.25*0.8)^(1/3) = 1.1447...
		/// </summary>
		/// <returns></returns>
		public double CalculateRoeRangeAverage(double minRoePercent, double maxRoePercent)
		{
			var ranges = new List<double>();
			for (int i = 0; i < Roes.Count - 1; i++)
			{
				var r0 = (double)Roes[i];
				var r1 = (double)Roes[i + 1];

				var roeRange =
					r0 < 0 && r1 > 0 ? // (-,+)
					Math.Clamp(r1, minRoePercent, maxRoePercent) / Math.Clamp(-r0, minRoePercent, maxRoePercent) :
					r0 > 0 && r1 < 0 ? // (+,-)
					Math.Clamp(r0, minRoePercent, maxRoePercent) / Math.Clamp(-r1, minRoePercent, maxRoePercent) :
					r0 > 0 && r1 > 0 ? // (+,+)
					Math.Clamp(r1, minRoePercent, maxRoePercent) / Math.Clamp(r0, minRoePercent, maxRoePercent) :
					// (-,-)
					Math.Clamp(-r0, minRoePercent, maxRoePercent) / Math.Clamp(-r1, minRoePercent, maxRoePercent);

				ranges.Add(roeRange);
			}
			return ArrayCalculator.GeometricMean([.. ranges]);
		}

		public override string ToString()
		{
			return string.Join(", ", Histories.Select(x => Calculator.Roe(Side, EntryPrice, x.Price) + "%"));
		}
	}
}
