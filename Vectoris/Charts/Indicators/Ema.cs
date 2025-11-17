using Vectoris.Charts.Core;

namespace Vectoris.Charts.Indicators
{
	/// <summary>
	/// Exponential Moving Average
	/// </summary>
	/// <param name="period"></param>
	public class Ema(int period) : IndicatorBase
	{
		private decimal? _lastEma = null;
		private int _quotesCount = 0;
		private Queue<decimal> _seedQueue = new();

		public override string Name => $"EMA({period})";
		public override decimal? Current => _lastEma;

		private decimal Alpha => 2m / (period + 1);

		public override void AddQuote(Quote quote)
		{
			decimal price = quote.Close;

			_quotesCount++;

			if (_quotesCount <= period)
			{
				_seedQueue.Enqueue(price);

				if (_quotesCount == period)
				{
					_lastEma = _seedQueue.Average();
					_values.Add(_lastEma);
				}
				else
				{
					_values.Add(null);
				}

				return;
			}

			_lastEma = Alpha * price + (1 - Alpha) * _lastEma!.Value;
			_values.Add(_lastEma);
		}
	}
}
