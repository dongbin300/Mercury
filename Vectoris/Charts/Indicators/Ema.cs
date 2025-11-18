using Vectoris.Charts.Core;

namespace Vectoris.Charts.Indicators;

/// <summary>
/// Exponential Moving Average
/// </summary>
/// <param name="period"></param>
[Indicator("EMA")]
public class Ema(int period) : IndicatorBase
{
	private decimal? _lastEma;
	private int _count = 0;
	private decimal _seedSum = 0;
	private readonly decimal _alpha = 2m / (period + 1);

	public override string Name => $"EMA({period})";
	public override decimal? Current => _lastEma;

	public override void AddQuote(Quote quote)
	{
		decimal price = quote.Close;
		_count++;

		if (_count <= period)
		{
			_seedSum += price;

			if (_count < period)
			{
				_values.Add(null);
				return;
			}

			_lastEma = _seedSum / period;
			_values.Add(_lastEma);
			return;
		}

		_lastEma = _alpha * price + (1m - _alpha) * _lastEma!.Value;
		_values.Add(_lastEma);
	}
}
