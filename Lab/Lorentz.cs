namespace Lab
{
	public class TradingModel
	{
		// Settings Object: General User-Defined Inputs
		public class Settings
		{
			public string Source { get; set; }
			public int NeighborsCount { get; set; }
			public int MaxBarsBack { get; set; }
			public int FeatureCount { get; set; }
			public int ColorCompression { get; set; }
			public bool ShowDefaultExits { get; set; }
			public bool UseDynamicExits { get; set; }
		}

		// Trade Stats Settings
		public bool ShowTradeStats { get; set; }
		public bool UseWorstCase { get; set; }

		// Filter Settings
		public class FilterSettings
		{
			public bool UseVolatilityFilter { get; set; }
			public bool UseRegimeFilter { get; set; }
			public bool UseAdxFilter { get; set; }
			public float RegimeThreshold { get; set; }
			public int AdxThreshold { get; set; }
		}

		// Feature Variables: User-Defined Inputs for calculating Feature Series
		public class FeatureSeries
		{
			public List<float> F1 { get; set; }
			public List<float> F2 { get; set; }
			public List<float> F3 { get; set; }
			public List<float> F4 { get; set; }
			public List<float> F5 { get; set; }
		}

		// Classification Labels
		public enum Direction
		{
			Long = 1,
			Short = -1,
			Neutral = 0
		}

		// Variables for storing predictions
		private List<int> yTrainArray = new List<int>();
		private List<float> predictions = new List<float>();
		private List<float> distances = new List<float>();
		private Direction signal = Direction.Neutral;
		private float lastDistance = -1f;

		// Kernel Regression Filters (Nadaraya-Watson)
		private bool useKernelFilter = true;
		private bool useKernelSmoothing = true;

		// Constructor
		public TradingModel()
		{
			// Initialize settings
			ShowTradeStats = true;
			UseWorstCase = false;
		}

		// Method to get Lorentzian Distance
		private float GetLorentzianDistance(int index, int featureCount, FeatureSeries featureSeries)
		{
			// Implement the logic to calculate Lorentzian distance
			// This is a placeholder, you need to add your own implementation here
			return 0.0f;
		}
		public double RationalQuadratic(double[] src, int lookback, double relativeWeight, int startAtBar)
		{
			double currentWeight = 0.0;
			double cumulativeWeight = 0.0;
			int size = src.Length;

			for (int i = 0; i < size + startAtBar; i++)
			{
				if (i >= size) break; // 배열 범위 초과 방지

				double y = src[i];
				double w = Math.Pow(1 + (Math.Pow(i, 2) / ((Math.Pow(lookback, 2) * 2 * relativeWeight))), -relativeWeight);

				currentWeight += y * w;
				cumulativeWeight += w;
			}

			return cumulativeWeight != 0 ? currentWeight / cumulativeWeight : 0; // 0으로 나누는 오류 방지
		}

		// Method to apply filters
		private bool ApplyFilters(bool volatility, bool regime, bool adx)
		{
			return volatility && regime && adx;
		}

		// Method to calculate Bar-Count Filters
		private bool CalculateBarCountFilters(Direction signal)
		{
			// Bar-Count Filters: Represents strict filters based on a pre-defined holding period of 4 bars
			int barsHeld = 0;
			barsHeld = signal != Direction.Neutral ? 0 : barsHeld + 1;
			bool isHeldFourBars = barsHeld == 4;
			bool isHeldLessThanFourBars = barsHeld > 0 && barsHeld < 4;

			return isHeldFourBars;
		}

		// Method to run the machine learning logic and apply filters
		public void RunMLLogic(Settings settings, FeatureSeries featureSeries, FilterSettings filterSettings)
		{
			// Define the y_train_series based on the source
			var src = settings.Source;
			var yTrainSeries = src[4] < src[0] ? Direction.Short : src[4] > src[0] ? Direction.Long : Direction.Neutral;

			yTrainArray.Add((int)yTrainSeries);

			// Variables used for ML Logic
			for (int i = 0; i < Math.Min(settings.MaxBarsBack - 1, yTrainArray.Count - 1); i++)
			{
				// Calculate the Lorentzian distance
				float d = GetLorentzianDistance(i, settings.FeatureCount, featureSeries);

				if (d >= lastDistance && i % 4 == 0)
				{
					lastDistance = d;
					distances.Add(d);
					predictions.Add(Math.Round(yTrainArray[i]));

					if (predictions.Count > settings.NeighborsCount)
					{
						lastDistance = distances[(int)(settings.NeighborsCount * 3 / 4)];
						distances.RemoveAt(0);
						predictions.RemoveAt(0);
					}
				}
			}

			// Apply prediction filters
			bool filterAll = ApplyFilters(filterSettings.UseVolatilityFilter, filterSettings.UseRegimeFilter, filterSettings.UseAdxFilter);
			if (filterAll)
			{
				if (predictions.Count > 0)
				{
					float prediction = 0f;
					foreach (var p in predictions)
					{
						prediction += p;
					}

					signal = prediction > 0 ? Direction.Long : prediction < 0 ? Direction.Short : Direction.Neutral;
				}
			}

			// Apply Bar-Count Filters
			bool isHeldFourBars = CalculateBarCountFilters(signal);
			if (isHeldFourBars)
			{
				// Logic for holding for four bars, you can implement additional logic here
			}

			// Implement Fractal Filters
			bool isDifferentSignalType = false; // Placeholder logic, adjust based on signal history
			bool isNewBuySignal = signal == Direction.Long && isDifferentSignalType;
			bool isNewSellSignal = signal == Direction.Short && isDifferentSignalType;

			// Example usage of kernel regression filters
			var yhat1 = RationalQuadratic(new float[] { 1.0f, 2.0f, 3.0f }, 3, 3); // Example usage
			bool isBullishRate = yhat1 < 1; // Placeholder logic for rate calculation

			// Display the kernel regression result
			Console.WriteLine($"Kernel Estimate: {yhat1}");
		}

		// Example usage of the TradingModel class
		public static void Main(string[] args)
		{
			var tradingModel = new TradingModel();

			// Initialize Settings
			var settings = new Settings
			{
				Source = "close", // Example, you may change this based on data
				NeighborsCount = 8,
				MaxBarsBack = 2000,
				FeatureCount = 5,
				ColorCompression = 1,
				ShowDefaultExits = false,
				UseDynamicExits = false
			};

			// Example feature series (You would populate these with actual data)
			var featureSeries = new FeatureSeries
			{
				F1 = new List<float> { 1.0f, 2.0f, 3.0f }, // Sample data for F1
				F2 = new List<float> { 2.0f, 3.0f, 4.0f }, // Sample data for F2
				F3 = new List<float> { 3.0f, 4.0f, 5.0f }, // Sample data for F3
				F4 = new List<float> { 4.0f, 5.0f, 6.0f }, // Sample data for F4
				F5 = new List<float> { 5.0f, 6.0f, 7.0f }  // Sample data for F5
			};

			// Example filter settings
			var filterSettings = new FilterSettings
			{
				UseVolatilityFilter = true,
				UseRegimeFilter = true,
				UseAdxFilter = true,
				RegimeThreshold = 50f,
				AdxThreshold = 25
			};

			// Running ML logic
			tradingModel.RunMLLogic(settings, featureSeries, filterSettings);
			//호오인쿄오마키세크리스즈하시다루
		}
	}
}