namespace Mercury.Maths
{
	/// <summary>
	/// For MLMI-Pivot
	/// </summary>
	public class KnnPredictionData4
	{
		public List<double?> Parameter1 { get; set; } = [];
		public List<double?> Parameter2 { get; set; } = [];
		public List<double?> Parameter3 { get; set; } = [];
		public List<double?> Parameter4 { get; set; } = [];
		public List<double?> PriceArray { get; set; } = [];
		public List<double?> ResultArray { get; set; } = [];

		public void StorePreviousTrade(double? p1, double? p2, double? p3, double? p4, double price, int n)
		{
			var prevPrice = PriceArray.Count > 0 ? PriceArray[^1] : price;

			Parameter1.Add(p1);
			Parameter2.Add(p2);
			Parameter3.Add(p3);
			Parameter4.Add(p4);
			PriceArray.Add(price);
			ResultArray.Add(price >= prevPrice ? 1 : -1);

			if (PriceArray.Count > n)
			{
				Parameter1.RemoveAt(0);
				Parameter2.RemoveAt(0);
				Parameter3.RemoveAt(0);
				Parameter4.RemoveAt(0);
				PriceArray.RemoveAt(0);
				ResultArray.RemoveAt(0);
			}
		}

		public double? KnnPredict(double? p1, double? p2, double? p3, double? p4, int k)
		{
			if (p1 == null || p2 == null || p3 == null || p4 == null)
			{
				return null;
			}

			var distances = new List<double?>();
			int n = Parameter1.Count;

			for (int i = 0; i < n; i++)
			{
				if (Parameter1[i] == null || Parameter2[i] == null || Parameter3[i] == null || Parameter4[i] == null)
				{
					distances.Add(null);
					continue;
				}
				double distance = Math.Sqrt(
					Math.Pow((p1 ?? 0) - (Parameter1[i] ?? 0), 2) +
					Math.Pow((p2 ?? 0) - (Parameter2[i] ?? 0), 2) +
					Math.Pow((p3 ?? 0) - (Parameter3[i] ?? 0), 2) +
					Math.Pow((p4 ?? 0) - (Parameter4[i] ?? 0), 2));
				distances.Add(distance);
			}

			var sortedIndices = distances.Select((value, index) => new { value, index }).Where(x => x.value != null).OrderBy(x => x.value).Select(x => x.index).ToList();
			var neighbors = sortedIndices.Take(k).Select(index => ResultArray[index]).OfType<double>().ToList();
			if (neighbors.Count == 0)
			{
				return 0;
			}
			var prediction = neighbors.Average();

			return prediction;
		}

		public double?[] Rescale(double?[] sourceSeries, double? oldMin, double? oldMax, double? newMin, double? newMax)
		{
			if (oldMin == null || oldMax == null || newMin == null || newMax == null)
			{
				return [.. sourceSeries.Select(_ => (double?)null)];
			}

			var oldMinValue = oldMin.Value;
			var oldMaxValue = oldMax.Value;
			var newMinValue = newMin.Value;
			var newMaxValue = newMax.Value;

			return [.. sourceSeries.Select(sourceValue =>
				sourceValue == null ? (double?)null :
				newMinValue + (newMaxValue - newMinValue) * (sourceValue.Value - oldMinValue) / Math.Max(oldMaxValue - oldMinValue, 1e-10)
			)];
		}

		public (bool[], bool[]) DetectPivots(double[] high, double[] low, int pivotBars)
		{
			var ph = ArrayCalculator.PivotHigh(high, pivotBars, pivotBars);
			var pl = ArrayCalculator.PivotLow(low, pivotBars, pivotBars);

			var phBool = ph.Select(x => x.HasValue).ToArray();
			var plBool = pl.Select(x => x.HasValue).ToArray();

			return (phBool, plBool);
		}
	}
}
