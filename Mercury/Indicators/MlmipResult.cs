namespace Mercury.Indicators
{
	/// <summary>
	/// Machine Learning Momentum Index with Pivot Result
	/// </summary>
	public class MlmipResult(DateTime date, double? prediction, double? predictionMa)
	{
		public DateTime Date { get; set; } = date;
		public double? Prediction { get; set; } = prediction;
		public double? PredictionMa { get; set; } = predictionMa;
	}
}
