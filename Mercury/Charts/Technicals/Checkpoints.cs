namespace Mercury.Charts.Technicals
{
	public class Checkpoints()
	{
		public IList<Checkpoint> Points { get; set; } = [];
		public IList<Checkpoint> HighPoints => [.. Points.Where(x => x.Position == CheckpointPosition.High)];
		public IList<Checkpoint> LowPoints => [.. Points.Where(x => x.Position == CheckpointPosition.Low)];
		public Checkpoint LastHighPoints => HighPoints.Last();
		public Checkpoint LastLowPoints => LowPoints.Last();

		public void EvaluateCheckpoint(List<ChartInfo> charts, int compareCandleCount)
		{
			for (int i = compareCandleCount; i < charts.Count - compareCandleCount; i++)
			{
				var time = charts[i].DateTime;
				var high = charts[i].Quote.High;
				var low = charts[i].Quote.Low;

				bool isHighPeak = true;
				bool isLowPeak = true;

				for (int j = i - compareCandleCount; j <= i + compareCandleCount; j++)
				{
					if (j != i)
					{
						var prevHigh = charts[j].Quote.High;
						var prevLow = charts[j].Quote.Low;

						if (high <= prevHigh)
						{
							isHighPeak = false;
						}

						if (low >= prevLow)
						{
							isLowPeak = false;
						}
					}
				}

				if (isHighPeak)
				{
					Points.Add(new Checkpoint(time, CheckpointPosition.High, high));
				}

				if (isLowPeak)
				{
					Points.Add(new Checkpoint(time, CheckpointPosition.Low, low));
				}
			}
		}

		public void ArrangePoints()
		{
			var newHistories = new List<Checkpoint>
			{
				Points[0]
			};

			for (int i = 1; i < Points.Count; i++)
			{
				var checkpoint = Points[i];
				var prevCheckpoint = Points[i - 1];

				if (checkpoint.Position != prevCheckpoint.Position || i == Points.Count - 1)
				{
					newHistories.Add(checkpoint);
				}
			}

			Points = newHistories;
		}
	}
}
