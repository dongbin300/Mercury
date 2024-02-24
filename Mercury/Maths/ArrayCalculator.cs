namespace Mercury.Maths
{
	public class ArrayCalculator
	{
		public static readonly double NA = 0;

		public static double Na(double value)
		{
			return value == 0 ? 0 : value;
		}

		public static double Nz(double value, double replacement = 0)
		{
			return value == 0 ? replacement : value;
		}

		/// <summary>
		/// NaN value -> previous nearest non-NaN value
		/// </summary>
		/// <param name="values"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static double FixNan(double[] values, int index)
		{
			for (int i = index; i >= 0; i--)
			{
				if (values[i] != 0)
				{
					return values[i];
				}
			}
			return 0;
		}

		/// <summary>
		/// Get minimum value
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		/// <param name="startIndex"></param>
		/// <returns></returns>
		public static double Min(double[] values, int count, int startIndex = 0)
		{
			double min = 999999999;
			for (int i = startIndex; i < startIndex + count; i++)
			{
				if (values[i] == NA)
				{
					continue;
				}

				if (values[i] < min)
				{
					min = values[i];
				}
			}
			return min;
		}

		/// <summary>
		/// Get maximum value
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		/// <param name="startIndex"></param>
		/// <returns></returns>
		public static double Max(double[] values, int count, int startIndex = 0)
		{
			var max = values[startIndex];
			for (int i = startIndex + 1; i < startIndex + count; i++)
			{
				if (values[i] == NA)
				{
					continue;
				}

				if (values[i] > max)
				{
					max = values[i];
				}
			}
			return max;
		}

		/// <summary>
		/// Get previous change value
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double[] Change(double[] values)
		{
			var result = new double[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				if (i == 0)
				{
					result[i] = NA;
					continue;
				}

				result[i] = values[i] - values[i - 1];
			}
			return result;
		}

		/// <summary>
		/// Simple average of values[0]~values[count-1]
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static double SAverage(double[] values, int count, int startIndex = 0)
		{
			double sum = 0;
			for (int i = startIndex; i < startIndex + count; i++)
			{
				sum += values[i];
			}
			return sum / count;
		}

		/// <summary>
		/// Relative average of values[0]~values[count-1]
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static double RAverage(double[] values, int count)
		{
			return default!;
		}

		/// <summary>
		/// Get geometric mean 
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double GeometricMean(double[] values)
		{
			double result = 1.0;
			for (int i = 0; i < values.Length; i++)
			{
				result *= Math.Pow(values[i], 1.0 / values.Length);
			}
			return result;
		}

		/// <summary>
		/// Recommend values is Quote.Close
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Sma(double[] values, int period)
		{
			var result = new double[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = NA;
					continue;
				}

				double sum = 0;
				for (int j = i - period + 1; j <= i; j++)
				{
					sum += values[j];
				}
				result[i] = sum / period;
			}

			return result;
		}

		/// <summary>
		/// Recommend values is Quote.Close
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Ema(double[] values, int period, int startIndex = 0)
		{
			var result = new double[values.Length];
			double alpha = 2.0 / (period + 1);
			for (int i = startIndex; i < values.Length; i++)
			{
				if (i < startIndex + period - 1)
				{
					result[i] = NA;
					continue;
				}

				if (i == startIndex + period - 1)
				{
					result[i] = SAverage(values, period, startIndex);
					continue;
				}

				result[i] = alpha * values[i] + (1 - alpha) * result[i - 1];
			}

			return result;
		}

		public static double[] Wma(double[] values, int period)
		{
			var result = new double[values.Length];

			/*
             * pine_wma(values, period) =>
    norm = 0.0
    sum = 0.0
    for i = 0 to period - 1
        weight = (period - i) * period
        norm := norm + weight
        sum := sum + values[i] * weight
    sum / norm
            */
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = NA;
					continue;
				}

				double norm = 0;
				double sum = 0;
				for (int j = 0; j < period; j++)
				{
					var weight = (period - j) * period;
					norm += weight;
					sum += values[i] * weight;
				}
				result[i] = sum / norm;
			}

			return result;
		}

		/// <summary>
		/// Recommend values is Quote.Close
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Rma(double[] values, int period, int startIndex = 0)
		{
			var result = new double[values.Length];
			double alpha = 1.0 / period;
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1 + startIndex)
				{
					result[i] = NA;
					continue;
				}

				if (i == period - 1 + startIndex)
				{
					result[i] = SAverage(values, period, startIndex);
					continue;
				}

				result[i] = alpha * values[i] + (1 - alpha) * result[i - 1];
			}

			return result;
		}

		/// <summary>
		/// Standard Deviation
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Stdev(double[] values, int period)
		{
			var result = new double[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = NA;
					continue;
				}

				double sum = 0;
				var avg = SAverage(values, period, i - period + 1);
				for (int j = i - period + 1; j < i + 1; j++)
				{
					sum += (values[j] - avg) * (values[j] - avg);
				}

				result[i] = Math.Sqrt(sum / period);
			}

			return result;
		}

		public static (double[], double[], double[], double[], double[]) IchimokuCloud(double[] high, double[] low, double[] close, int conversionPeriod, int basePeriod, int leadingSpanPeriod)
		{
			var displacementOffset = basePeriod - 1;
			var conversion = new double[high.Length];
			var _base = new double[high.Length];
			var trailingSpan = new double[high.Length];
			var leadingSpan1 = new double[high.Length];
			var leadingSpan2 = new double[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i < conversionPeriod - 1)
				{
					conversion[i] = NA;
				}
				else
				{
					conversion[i] = (Max(high, conversionPeriod, i + 1 - conversionPeriod) + Min(low, conversionPeriod, i + 1 - conversionPeriod)) / 2;
				}

				if (i < basePeriod - 1)
				{
					_base[i] = NA;
				}
				else
				{
					_base[i] = (Max(high, basePeriod, i + 1 - basePeriod) + Min(low, basePeriod, i + 1 - basePeriod)) / 2;
				}

				trailingSpan[i] = close[i];

				if (i < basePeriod - 1)
				{
					leadingSpan1[i] = NA;
				}
				else
				{
					leadingSpan1[i] = (conversion[i] + _base[i]) / 2;
				}

				if (i < leadingSpanPeriod - 1)
				{
					leadingSpan2[i] = NA;
				}
				else
				{
					leadingSpan2[i] = (Max(high, leadingSpanPeriod, i + 1 - leadingSpanPeriod) + Min(low, leadingSpanPeriod, i + 1 - leadingSpanPeriod)) / 2;
				}
			}

			// Trailing span : shift left
			var displacementTrailingSpan = new double[high.Length];
			Array.Copy(trailingSpan, displacementOffset, displacementTrailingSpan, 0, high.Length - displacementOffset);
			// Fill rest with last value
			Array.Fill(displacementTrailingSpan, displacementTrailingSpan[high.Length - displacementOffset - 1], high.Length - displacementOffset, displacementOffset);

			// Leading span 1,2 : shift right
			var displacementLeadingSpan1 = new double[high.Length + displacementOffset];
			Array.Copy(leadingSpan1, 0, displacementLeadingSpan1, displacementOffset, high.Length);
			var displacementLeadingSpan2 = new double[high.Length + displacementOffset];
			Array.Copy(leadingSpan2, 0, displacementLeadingSpan2, displacementOffset, high.Length);

			return (conversion, _base, displacementTrailingSpan, displacementLeadingSpan1, displacementLeadingSpan2);
		}

		/// <summary>
		/// Bollinger Bands
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <param name="deviation"></param>
		/// <returns></returns>
		public static (double[], double[], double[]) BollingerBands(double[] values, int period, double deviation)
		{
			var sma = Sma(values, period);
			var upper = new double[values.Length];
			var lower = new double[values.Length];
			var stdev = Stdev(values, period);
			for (int i = 0; i < values.Length; i++)
			{
				var dev = deviation * stdev[i];
				upper[i] = sma[i] + dev;
				lower[i] = sma[i] - dev;
			}
			return (sma, upper, lower);
		}

		/// <summary>
		/// EMA based Moving Average Convergence Divergence(MACD)
		/// </summary>
		/// <param name="values"></param>
		/// <param name="fastPeriod"></param>
		/// <param name="slowPeriod"></param>
		/// <param name="signalPeriod"></param>
		/// <returns></returns>
		public static (double[], double[], double[]) Macd(double[] values, int fastPeriod, int slowPeriod, int signalPeriod)
		{
			var fast = Ema(values, fastPeriod);
			var slow = Ema(values, slowPeriod);
			var macd = new double[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < slowPeriod - 1)
				{
					macd[i] = NA;
					continue;
				}

				macd[i] = fast[i] - slow[i];
			}
			var signal = Ema(macd, signalPeriod, slowPeriod - 1);
			var hist = new double[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < slowPeriod + signalPeriod - 2)
				{
					hist[i] = NA;
					continue;
				}

				hist[i] = macd[i] - signal[i];
			}

			return (macd, signal, hist);
		}

		/// <summary>
		/// True Range
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		public static double[] Tr(double[] high, double[] low, double[] close)
		{
			var tr = new double[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i == 0)
				{
					tr[i] = high[i] - low[i];
					continue;
				}

				tr[i] =
					Math.Max(
						Math.Max(high[i] - low[i], Math.Abs(high[i] - close[i - 1])),
						Math.Abs(low[i] - close[i - 1])
						);
			}

			return tr;
		}

		/// <summary>
		/// Average True Range
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Atr(double[] high, double[] low, double[] close, int period, int startIndex = 0)
		{
			var tr = Tr(high, low, close);
			return Rma(tr, period, startIndex);
		}

		/// <summary>
		/// Relative Strength Index
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Rsi(double[] values, int period)
		{
			var rsi = new double[values.Length];
			var u = new double[values.Length];
			var d = new double[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				if (i == 0)
				{
					u[i] = NA;
					d[i] = NA;
					continue;
				}

				u[i] = Math.Max(values[i] - values[i - 1], 0);
				d[i] = Math.Max(values[i - 1] - values[i], 0);
			}

			var uRma = Rma(u, period, 1);
			var dRma = Rma(d, period, 1);
			for (int i = period; i < values.Length; i++)
			{
				var rs = uRma[i] / dRma[i];
				rsi[i] = 100 - 100 / (1 + rs);
			}

			return rsi;
		}

		/// <summary>
		/// Stochastic
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Stoch(double[] high, double[] low, double[] close, int period, int startIndex = 0)
		{
			var stoch = new double[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i < period - 1 + startIndex)
				{
					stoch[i] = NA;
					continue;
				}

				var min = Min(low, period, i + 1 - period);
				var max = Max(high, period, i + 1 - period);
				stoch[i] = 100 * (close[i] - min) / (max - min);
			}

			return stoch;
		}

		/// <summary>
		/// Direction
		/// -1 - uptrend
		/// 1 - downtrend
		/// 
		/// atrPeriod 이전의 값들은 의미가 없는 것 같음
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <param name="factor"></param>
		/// <param name="atrPeriod"></param>
		/// <returns></returns>
		public static (double[], double[]) Supertrend(double[] high, double[] low, double[] close, double factor, int atrPeriod)
		{
			var upperBand = new double[high.Length];
			var lowerBand = new double[high.Length];
			var supertrend = new double[high.Length];
			var direction = new double[high.Length];

			var atr = Atr(high, low, close, atrPeriod);
			for (int i = 0; i < high.Length; i++)
			{
				var mid = (high[i] + low[i]) / 2;
				upperBand[i] = mid + factor * atr[i];
				lowerBand[i] = mid - factor * atr[i];
				var prevUpperBand = i == 0 ? 0 : upperBand[i - 1];
				var prevLowerBand = i == 0 ? 0 : lowerBand[i - 1];
				var prevClose = i == 0 ? 0 : close[i - 1];
				var prevSupertrend = i == 0 ? 0 : supertrend[i - 1];

				lowerBand[i] = lowerBand[i] > prevLowerBand || prevClose < prevLowerBand ? lowerBand[i] : prevLowerBand;
				upperBand[i] = upperBand[i] < prevUpperBand || prevClose > prevUpperBand ? upperBand[i] : prevUpperBand;

				direction[i] =
					i == 0 ? 1 :
					prevSupertrend == prevUpperBand ? (close[i] > upperBand[i] ? -1 : 1) :
					(close[i] < lowerBand[i] ? 1 : -1);

				supertrend[i] = direction[i] == -1 ? lowerBand[i] : upperBand[i];
			}

			return (supertrend, direction);
		}

		public static (double[], double[]) ReverseSupertrend(double[] high, double[] low, double[] close, double factor, int atrPeriod)
		{
			var upperBand = new double[high.Length];
			var lowerBand = new double[high.Length];
			var supertrend = new double[high.Length];
			var direction = new double[high.Length];

			var atr = Atr(high, low, close, atrPeriod);
			for (int i = 0; i < high.Length; i++)
			{
				var mid = (high[i] + low[i]) / 2;
				upperBand[i] = mid + factor * atr[i];
				lowerBand[i] = mid - factor * atr[i];
				var prevUpperBand = i == 0 ? 0 : upperBand[i - 1];
				var prevLowerBand = i == 0 ? 0 : lowerBand[i - 1];
				var prevClose = i == 0 ? 0 : close[i - 1];
				var prevSupertrend = i == 0 ? 0 : supertrend[i - 1];

				lowerBand[i] = lowerBand[i] > prevLowerBand || prevClose < prevLowerBand ? lowerBand[i] : prevLowerBand;
				upperBand[i] = upperBand[i] < prevUpperBand || prevClose > prevUpperBand ? upperBand[i] : prevUpperBand;

				direction[i] =
					i == 0 ? 1 :
					prevSupertrend == prevLowerBand ? (close[i] > upperBand[i] ? -1 : 1) :
					(close[i] < lowerBand[i] ? 1 : -1);

				supertrend[i] = direction[i] == -1 ? upperBand[i] : lowerBand[i];
			}

			return (supertrend, direction);
		}

		public static (double[], double[], double[], double[], double[], double[]) TripleSupertrend(double[] high, double[] low, double[] close, int atrPeriod1, double factor1, int atrPeriod2, double factor2, int atrPeriod3, double factor3)
		{
			(var supertrend1, var direction1) = Supertrend(high, low, close, factor1, atrPeriod1);
			(var supertrend2, var direction2) = Supertrend(high, low, close, factor2, atrPeriod2);
			(var supertrend3, var direction3) = Supertrend(high, low, close, factor3, atrPeriod3);

			return (supertrend1, direction1, supertrend2, direction2, supertrend3, direction3);
		}

		public static (double[], double[]) StochasticRsi(double[] close, int smoothK, int smoothD, int rsiPeriod, int stochasticPeriod)
		{
			var rsi = Rsi(close, rsiPeriod);
			var stoch = Stoch(rsi, rsi, rsi, stochasticPeriod, 2);
			var k = Sma(stoch, smoothK);
			var d = Sma(k, smoothD);

			return (k, d);
		}

		public static double[] Adx(double[] high, double[] low, double[] close, int adxPeriod, int diPeriod)
		{
			var adx = new double[high.Length];
			var up = new double[high.Length];
			var down = new double[high.Length];
			var plusDm = new double[high.Length];
			var minusDm = new double[high.Length];
			var trueRange = new double[high.Length];
			var _plus = new double[high.Length];
			var _minus = new double[high.Length];
			var diff2 = new double[high.Length];

			up = Change(high);
			down = Change(low).Select(x => -x).ToArray();
			for (int i = 0; i < high.Length; i++)
			{
				if (i == 0)
				{
					plusDm[i] = NA;
					minusDm[i] = NA;
					continue;
				}

				plusDm[i] = (up[i] > down[i] && up[i] > 0) ? up[i] : 0;
				minusDm[i] = (up[i] < down[i] && down[i] > 0) ? down[i] : 0;
			}
			trueRange = Atr(high, low, close, diPeriod, 1);
			var __plus = Rma(plusDm, diPeriod, 1);
			var __minus = Rma(minusDm, diPeriod, 1);
			for (int i = 0; i < high.Length; i++)
			{
				if (trueRange[i] == 0)
				{
					_plus[i] = 0;
					_minus[i] = 0;
				}
				else
				{
					_plus[i] = 100 * __plus[i] / trueRange[i];
					_minus[i] = 100 * __minus[i] / trueRange[i];
				}
			}
			for (int i = 0; i < high.Length; i++)
			{
				var plus = FixNan(_plus, i);
				var minus = FixNan(_minus, i);
				var sum = plus + minus;
				var diff = Math.Abs(plus - minus);
				diff2[i] = diff / (sum == 0 ? 1 : sum);
			}

			var _adx = Rma(diff2, adxPeriod, diPeriod);
			for (int i = 0; i < high.Length; i++)
			{
				adx[i] = 100 * _adx[i];
			}

			return adx;
		}

		public static double[] Smma(double[] values, int period)
		{
			var smma = new double[values.Length];

			smma[0] = values[0];
			for (int i = 1; i < values.Length; i++)
			{
				smma[i] = (smma[i - 1] * (period - 1) + values[i]) / period;
			}

			return smma;
		}

		public static double[] Zlema(double[] values, int period)
		{
			var zlema = new double[values.Length];

			var ema1 = Ema(values, period);
			var ema2 = Ema(ema1, period);
			for (int i = 0; i < values.Length; i++)
			{
				zlema[i] = 2 * ema1[i] - ema2[i];
			}

			return zlema;
		}

		/// <summary>
		/// Impulse MACD
		/// Need more test
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] Imacd(double[] high, double[] low, double[] close, int period)
		{
			var result = new double[high.Length];
			var hlc = new double[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				hlc[i] = (high[i] + low[i] + close[i]) / 3;
			}

			var hi = Smma(high, period);
			var lo = Smma(low, period);
			var mi = Zlema(hlc, period);

			for (int i = 0; i < high.Length; i++)
			{
				if (mi[i] > hi[i])
				{
					result[i] = mi[i] - hi[i];
				}
				else if (mi[i] < lo[i])
				{
					result[i] = mi[i] - lo[i];
				}
				else
				{
					result[i] = 0;
				}
			}

			return result;
		}

		/// <summary>
		/// Need more test
		/// </summary>
		/// <param name="close"></param>
		/// <param name="volume"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double[] TimeSegmentedVolume(double[] close, double[] volume, int period)
		{
			var tsv = new double[close.Length];
			for (int i = 0; i < close.Length; i++)
			{
				if (i < period)
				{
					tsv[i] = 0;
					continue;
				}

				double sum = 0;
				for (int j = 0; j < period; j++)
				{
					sum += volume[i - j] * (close[i - j] - close[i - j - 1]);
				}
				tsv[i] = sum;
			}
			return tsv;
		}

		public static double[] Custom(double[] open, double[] high, double[] low, double[] close, double[] volume, int period)
		{
			var custom = new double[open.Length];
			for (int i = 0; i < open.Length; i++)
			{
				custom[i] = (high[i] + low[i]) / 2;
			}
			return custom;
		}
	}
}
