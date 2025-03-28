namespace Mercury.Maths
{
	/// <summary>
	/// For MLMI(Machine Learning Momentum Index)
	/// </summary>
	public class KnnPredictionData2
	{
		public List<double?> Parameter1 { get; set; } = [];
		public List<double?> Parameter2 { get; set; } = [];
		public List<double?> PriceArray { get; set; } = [];
		public List<double?> ResultArray { get; set; } = [];

		public void StorePreviousTrade(double? p1, double? p2, double price)
		{
			ResultArray.Add(price >= PriceArray[^1] ? 1 : -1);
			Parameter1.Add(p1);
			Parameter2.Add(p2);
			PriceArray.Add(price);
		}

		public double? KnnPredict(double? p1, double? p2, int k)
		{
			if (p1 == null || p2 == null)
			{
				return null;
			}

			var distances = new List<double?>();
			int n = Parameter1.Count;

			for (int i = 0; i < n; i++)
			{
				if (Parameter1[i] == null || Parameter2[i] == null)
				{
					distances.Add(null);
					continue;
				}
				double distance = Math.Sqrt(
					Math.Pow(p1 ?? 0 - Parameter1[i] ?? 0, 2) +
					Math.Pow(p2 ?? 0 - Parameter2[i] ?? 0, 2));
				distances.Add(distance);
			}

			var sortedDistances = distances.OrderBy(x => x).Take(k).ToList();
			double maxDist = sortedDistances.Max() ?? 0;

			var neighbors = new List<double>();
			for (int i = 0; i < distances.Count; i++)
			{
				if (ResultArray[i] == null)
				{
					continue;
				}

				if (distances[i] <= maxDist)
				{
					neighbors.Add(ResultArray[i] ?? 0);
				}
			}

			return neighbors.Sum();
		}
	}
}
