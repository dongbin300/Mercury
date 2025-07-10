namespace Mercury.Maths
{
	public class StatisticalMath
	{
		/// <summary>
		/// 피어슨 상관계수(Pearson Correlation Coefficient)를 계산합니다.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static double PearsonCorrelation(double[] x, double[] y)
		{
			if (x.Length != y.Length)
			{
				throw new ArgumentException("Array length not same.");
			}
			int n = x.Length;
			double avgX = x.Average();
			double avgY = y.Average();

			double sumXY = 0, sumX2 = 0, sumY2 = 0;
			for (int i = 0; i < n; i++)
			{
				double dx = x[i] - avgX;
				double dy = y[i] - avgY;
				sumXY += dx * dy;
				sumX2 += dx * dx;
				sumY2 += dy * dy;
			}
			double denominator = Math.Sqrt(sumX2 * sumY2);
			return denominator == 0 ? 0 : sumXY / denominator;
		}

		/// <summary>
		/// 스피어만 상관계수(Spearman Correlation Coefficient)를 계산합니다.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static double SpearmanCorrelation(double[] x, double[] y)
		{
			if (x.Length != y.Length)
			{
				throw new ArgumentException("Array length not same.");
			}
			int n = x.Length;
			double[] rankX = GetRanks(x);
			double[] rankY = GetRanks(y);

			double dSum = 0;
			for (int i = 0; i < n; i++)
			{
				double d = rankX[i] - rankY[i];
				dSum += d * d;
			}
			double denominator = (double)n * (n * n - 1);

			return 1 - (6 * dSum) / denominator;
		}

		/// <summary>
		/// 주어진 값들의 순위를 계산합니다. 동점은 평균 순위를 부여합니다.
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		private static double[] GetRanks(double[] values)
		{
			int n = values.Length;
			var sorted = values
				.Select((v, i) => new { Value = v, Index = i })
				.OrderByDescending(x => x.Value)
				.ToList();

			double[] ranks = new double[n];
			int i = 0;
			while (i < n)
			{
				int j = i;
				while (j + 1 < n && sorted[j + 1].Value == sorted[i].Value) j++;
				double rank = (i + j + 2) / 2.0; // 1-based rank
				for (int k = i; k <= j; k++)
					ranks[sorted[k].Index] = rank;
				i = j + 1;
			}
			return ranks;
		}

	}
}
