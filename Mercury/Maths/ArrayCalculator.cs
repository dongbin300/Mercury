﻿using Mercury.Extensions;

using System.Data;
using System.Drawing;

namespace Mercury.Maths
{
	/// <summary>
	/// TODO 
	/// Mean Reversion
	/// Z-score
	/// QQE MOD
	/// 
	/// </summary>
	public class ArrayCalculator
	{
		public static readonly double NA = 0;
		public static readonly bool NAb = false;

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
		public static double FixNan(double?[] values, int index)
		{
			for (int i = index; i >= 0; i--)
			{
				if (values[i] != null)
				{
					return values[i] ?? 0;
				}
			}
			return 0;
		}

		public static double?[] Add(double?[] values, double addend)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				result[i] = values[i].HasValue ? values[i] + addend : null;
			}
			return result;
		}

		public static double?[] Add(double?[] values1, double?[] values2)
		{
			int length = Math.Min(values1.Length, values2.Length);
			var result = new double?[length];

			for (int i = 0; i < length; i++)
			{
				if (values1[i].HasValue && values2[i].HasValue)
				{
					result[i] = values1[i] + values2[i];
				}
				else
				{
					result[i] = null;
				}
			}

			return result;
		}

		public static double?[] Subtract(double?[] values, double subtrahend)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				result[i] = values[i].HasValue ? values[i] - subtrahend : null;
			}
			return result;
		}

		public static double?[] Subtract(double?[] values1, double?[] values2)
		{
			int length = Math.Min(values1.Length, values2.Length);
			var result = new double?[length];

			for (int i = 0; i < length; i++)
			{
				result[i] = (values1[i].HasValue && values2[i].HasValue) ? values1[i] - values2[i] : null;
			}

			return result;
		}

		public static double?[] Multiply(double?[] values, double multiplier)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				result[i] = values[i].HasValue ? values[i] * multiplier : null;
			}
			return result;
		}

		public static double?[] Multiply(double?[] values1, double?[] values2)
		{
			int length = Math.Min(values1.Length, values2.Length);
			var result = new double?[length];

			for (int i = 0; i < length; i++)
			{
				result[i] = (values1[i].HasValue && values2[i].HasValue) ? values1[i] * values2[i] : null;
			}

			return result;
		}

		public static double?[] Divide(double?[] values, double divisor)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].HasValue)
				{
					result[i] = divisor != 0 ? values[i] / divisor : null;
				}
				else
				{
					result[i] = null;
				}
			}
			return result;
		}

		public static double?[] Divide(double?[] values1, double?[] values2)
		{
			int length = Math.Min(values1.Length, values2.Length);
			var result = new double?[length];

			for (int i = 0; i < length; i++)
			{
				if (values1[i].HasValue && values2[i].HasValue)
				{
					result[i] = (values2[i] != 0) ? values1[i] / values2[i] : null;
				}
				else
				{
					result[i] = null;
				}
			}

			return result;
		}

		public static bool?[] GreaterThan(double?[] a, double?[] b)
		{
			var result = new bool?[a.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (!a[i].HasValue || !b[i].HasValue)
				{
					result[i] = null;
				}
				else
				{
					result[i] = a[i] > b[i];
				}
			}

			return result;
		}

		public static bool?[] LessThan(double?[] a, double?[] b)
		{
			var result = new bool?[a.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (!a[i].HasValue || !b[i].HasValue)
				{
					result[i] = null;
				}
				else
				{
					result[i] = a[i] < b[i];
				}
			}

			return result;
		}

		public static bool?[] And(bool?[] a, bool?[] b)
		{
			var result = new bool?[a.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (!a[i].HasValue || !b[i].HasValue)
				{
					result[i] = null;
				}
				else
				{
					result[i] = a[i] == true && b[i] == true;
				}
			}

			return result;
		}

		public static bool?[] Or(bool?[] a, bool?[] b)
		{
			var result = new bool?[a.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (!a[i].HasValue || !b[i].HasValue)
				{
					result[i] = null;
				}
				else
				{
					result[i] = a[i] == true || b[i] == true;
				}
			}

			return result;
		}

		public static bool?[] Not(bool?[] value)
		{
			var result = new bool?[value.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = !value[i];
			}

			return result;
		}

		/// <summary>
		/// Get minimum value
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		/// <param name="startIndex"></param>
		/// <returns></returns>
		public static double Min(double?[] values, int count, int startIndex = 0)
		{
			double min = double.MaxValue;
			for (int i = startIndex; i < startIndex + count; i++)
			{
				if (values[i].HasValue && values[i] < min)
				{
					min = values[i] ?? min;
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
		public static double Max(double?[] values, int count, int startIndex = 0)
		{
			double max = double.MinValue;
			for (int i = startIndex; i < startIndex + count; i++)
			{
				if (values[i].HasValue && values[i] > max)
				{
					max = values[i] ?? max;
				}
			}
			return max;
		}

		/// <summary>
		/// Get Lowest values
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Lowest(double[] values, int period)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}

				double min = double.MaxValue;
				for (int j = i; j > i - period; j--)
				{
					if (values[j] < min)
					{
						min = values[j];
					}
				}
				result[i] = min;
			}
			return result;
		}

		/// <summary>
		/// Get Highest Values
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Highest(double[] values, int period)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}

				double max = double.MinValue;
				for (int j = i; j > i - period; j--)
				{
					if (values[j] > max)
					{
						max = values[j];
					}
				}
				result[i] = max;
			}
			return result;
		}

		/// <summary>
		/// Get Average of values
		/// </summary>
		/// <param name="values1"></param>
		/// <param name="values2"></param>
		/// <returns></returns>
		public static double?[] Average(double?[] values1, double?[] values2)
		{
			var result = new double?[values1.Length];
			for (int i = 0; i < values1.Length; i++)
			{
				if (values1[i] == null || values2[i] == null)
				{
					result[i] = null;
					continue;
				}
				result[i] = (values1[i] + values2[i]) / 2;
			}
			return result;
		}

		/// <summary>
		/// Get previous change value
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double?[] Change(double?[] values)
		{
			var result = new double?[values.Length];

			result[0] = null;
			for (int i = 1; i < values.Length; i++)
			{
				result[i] = values[i] - values[i - 1];
			}
			return result;
		}

		/// <summary>
		/// Whether value1 cross over value2
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <returns></returns>
		public static bool?[] Crossover(double?[] value1, double?[] value2)
		{
			int length = Math.Min(value1.Length, value2.Length);
			if (length < 2)
			{
				return [];
			}

			var result = new bool?[length];

			result[0] = null;
			for (int i = 1; i < length; i++)
			{
				result[i] =
					value1[i - 1] != null && value1[i] != null &&
					value2[i - 1] != null && value2[i] != null &&
					(value1[i - 1] <= value2[i - 1]) && (value1[i] > value2[i]);
			}

			return result;
		}

		/// <summary>
		/// Whether value1 cross under value2
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <returns></returns>
		public static bool?[] Crossunder(double?[] value1, double?[] value2)
		{
			int length = Math.Min(value1.Length, value2.Length);
			if (length < 2)
			{
				return [];
			}

			var result = new bool?[length];

			result[0] = null;
			for (int i = 1; i < length; i++)
			{
				result[i] =
					value1[i - 1] != null && value1[i] != null &&
					value2[i - 1] != null && value2[i] != null &&
					(value1[i - 1] >= value2[i - 1]) && (value1[i] < value2[i]);
			}

			return result;
		}

		/// <summary>
		/// Simple average of values[0]~values[count-1]
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static double SAverage(double?[] values, int count, int startIndex = 0)
		{
			double sum = 0;
			for (int i = startIndex; i < startIndex + count; i++)
			{
				sum += values[i] ?? 0;
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
		public static double?[] Sma(double?[] values, int period)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}

				double sum = 0;
				bool isNull = false;
				for (int j = i - period + 1; j <= i; j++)
				{
					if (values[j] == null)
					{
						isNull = true;
						break;
					}
					sum += values[j] ?? 0;
				}
				result[i] = isNull ? null : sum / period;
			}

			return result;
		}

		/// <summary>
		/// Recommend values is Quote.Close
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Ema(double?[] values, int period, int startIndex = 0)
		{
			var result = new double?[values.Length];
			double alpha = 2.0 / (period + 1);
			for (int i = startIndex; i < values.Length; i++)
			{
				if (i < startIndex + period - 1)
				{
					result[i] = null;
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

		/// <summary>
		/// Recommend values is Quote.Close
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Wma(double?[] values, int period)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}

				double norm = 0;
				double sum = 0;
				bool isNull = false;
				for (int j = 0; j < period; j++)
				{
					var value = values[i - j];
					if (value == null)
					{
						isNull = true;
						break;
					}

					var weight = (period - j) * period;
					norm += weight;
					sum += (value ?? 0) * weight;
				}
				result[i] = isNull ? null : sum / norm;
			}

			return result;
		}

		/// <summary>
		/// Recommend values is Quote.Close
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Rma(double?[] values, int period, int startIndex = 0)
		{
			var result = new double?[values.Length];
			double alpha = 1.0 / period;
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1 + startIndex)
				{
					result[i] = null;
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

		public static double?[] Vwap(double?[] high, double?[] low, double?[] close, double?[] volume, int period)
		{
			var result = new double?[close.Length];
			for (int i = 0; i < close.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}
				double sum = 0;
				double volumeSum = 0;
				for (int j = i - period + 1; j < i + 1; j++)
				{
					sum += (close[j] ?? 0) * (high[j] ?? 0) * (low[j] ?? 0) * (volume[j] ?? 0);
					volumeSum += volume[j] ?? 0;
				}
				result[i] = sum / volumeSum;
			}
			return result;
		}

		/// <summary>
		/// Standard Deviation
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Stdev(double?[] values, int period)
		{
			var result = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}

				double sum = 0;
				var avg = SAverage(values, period, i - period + 1);
				for (int j = i - period + 1; j < i + 1; j++)
				{
					sum += ((values[j] ?? 0) - avg) * ((values[j] ?? 0) - avg);
				}

				result[i] = Math.Sqrt(sum / period);
			}

			return result;
		}

		/// <summary>
		/// Price of the pivot high point
		/// </summary>
		/// <param name="high"></param>
		/// <param name="leftBars"></param>
		/// <param name="rightBars"></param>
		/// <returns></returns>
		public static double?[] PivotHigh(double[] high, int leftBars, int rightBars)
		{
			int length = high.Length;
			var pivots = new double?[length];

			for (int pivotIndex = leftBars; pivotIndex < length - rightBars; pivotIndex++)
			{
				double pivotValue = high[pivotIndex];

				bool isPivot = high.Skip(pivotIndex - leftBars).Take(leftBars).All(x => x < pivotValue) &&
							   high.Skip(pivotIndex + 1).Take(rightBars).All(x => x < pivotValue);

				pivots[pivotIndex] = isPivot ? pivotValue : null;
			}

			return pivots;
		}

		/// <summary>
		/// Price of the pivot low point
		/// </summary>
		/// <param name="low"></param>
		/// <param name="leftBars"></param>
		/// <param name="rightBars"></param>
		/// <returns></returns>
		public static double?[] PivotLow(double[] low, int leftBars, int rightBars)
		{
			int length = low.Length;
			var pivots = new double?[length];

			for (int pivotIndex = leftBars; pivotIndex < length - rightBars; pivotIndex++)
			{
				double pivotValue = low[pivotIndex];

				bool isPivot = low.Skip(pivotIndex - leftBars).Take(leftBars).All(x => x > pivotValue) &&
							   low.Skip(pivotIndex + 1).Take(rightBars).All(x => x > pivotValue);

				pivots[pivotIndex] = isPivot ? pivotValue : null;
			}

			return pivots;
		}

		/// <summary>
		/// Linear Regression
		/// </summary>
		/// <param name="values"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static double? LinearRegression(double?[] src, int len, int startIndex, int offset = 0)
		{
			if (src == null || src.Length < startIndex + 1 || startIndex + 1 < len)
			{
				return null;
			}

			// x_sum과 xx_sum은 1번만 계산하면 되므로, 루프 밖에서 계산
			double x_sum = 0.0;
			double xx_sum = 0.0;
			for (int i = 0; i < len; i++)
			{
				x_sum += i;
				xx_sum += i * i;
			}

			// y_sum 계산 (src 배열에서 값들의 합)
			double y_sum = 0.0;
			double xy_sum = 0.0;
			for (int i = 0; i < len; i++)
			{
				int idx = startIndex - (len - 1) + i;
				if (src[idx].HasValue)
				{
					double y = src[idx].Value;
					y_sum += y;
					xy_sum += i * y; // x 값은 0부터 len-1
				}
				else
				{
					return null; // 하나라도 null이면 계산을 중단하고 null 반환
				}
			}

			// slope 계산
			double denominator = (len * xx_sum - x_sum * x_sum);
			if (denominator == 0)
			{
				return null; // 분모가 0이면 선형 회귀 값을 계산할 수 없으므로 null 반환
			}
			double slope = (len * xy_sum - x_sum * y_sum) / denominator;

			// intercept 계산
			double intercept = (y_sum - slope * x_sum) / len;

			// 선형 회귀 값 계산 (offset을 고려하여 계산)
			double linreg = intercept + slope * (len - 1 - offset);

			return linreg;
		}


		public static (double?[], double?[], double?[], double?[], double?[]) IchimokuCloud(double[] high, double[] low, double[] close, int conversionPeriod, int basePeriod, int leadingSpanPeriod)
		{
			var nHigh = high.ToNullable();
			var nLow = low.ToNullable();

			var displacementOffset = basePeriod - 1;
			var conversion = new double?[high.Length];
			var _base = new double?[high.Length];
			var trailingSpan = new double?[high.Length];
			var leadingSpan1 = new double?[high.Length];
			var leadingSpan2 = new double?[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i < conversionPeriod - 1)
				{
					conversion[i] = null;
				}
				else
				{
					conversion[i] = (Max(nHigh, conversionPeriod, i + 1 - conversionPeriod) + Min(nLow, conversionPeriod, i + 1 - conversionPeriod)) / 2;
				}

				if (i < basePeriod - 1)
				{
					_base[i] = null;
				}
				else
				{
					_base[i] = (Max(nHigh, basePeriod, i + 1 - basePeriod) + Min(nLow, basePeriod, i + 1 - basePeriod)) / 2;
				}

				trailingSpan[i] = close[i];

				if (i < basePeriod - 1)
				{
					leadingSpan1[i] = null;
				}
				else
				{
					leadingSpan1[i] = (conversion[i] + _base[i]) / 2;
				}

				if (i < leadingSpanPeriod - 1)
				{
					leadingSpan2[i] = null;
				}
				else
				{
					leadingSpan2[i] = (Max(nHigh, leadingSpanPeriod, i + 1 - leadingSpanPeriod) + Min(nLow, leadingSpanPeriod, i + 1 - leadingSpanPeriod)) / 2;
				}
			}

			// Trailing span : shift left
			var displacementTrailingSpan = new double?[high.Length];
			Array.Copy(trailingSpan, displacementOffset, displacementTrailingSpan, 0, high.Length - displacementOffset);
			// Fill rest with last value
			Array.Fill(displacementTrailingSpan, displacementTrailingSpan[high.Length - displacementOffset - 1], high.Length - displacementOffset, displacementOffset);

			// Leading span 1,2 : shift right
			var displacementLeadingSpan1 = new double?[high.Length + displacementOffset];
			Array.Copy(leadingSpan1, 0, displacementLeadingSpan1, displacementOffset, high.Length);
			var displacementLeadingSpan2 = new double?[high.Length + displacementOffset];
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
		public static (double?[], double?[], double?[]) BollingerBands(double?[] values, int period, double deviation)
		{
			var sma = Sma(values, period);
			var upper = new double?[values.Length];
			var lower = new double?[values.Length];
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
		public static (double?[], double?[], double?[]) Macd(double?[] values, int fastPeriod, int slowPeriod, int signalPeriod)
		{
			var fast = Ema(values, fastPeriod);
			var slow = Ema(values, slowPeriod);
			var macd = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < slowPeriod - 1)
				{
					macd[i] = null;
					continue;
				}

				macd[i] = fast[i] - slow[i];
			}
			var signal = Ema(macd, signalPeriod, slowPeriod - 1);
			var hist = new double?[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				if (i < slowPeriod + signalPeriod - 2)
				{
					hist[i] = null;
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
		public static double?[] Atr(double[] high, double[] low, double[] close, int period, int startIndex = 0)
		{
			var tr = Tr(high, low, close);
			return Rma(tr.ToNullable(), period, startIndex);
		}

		/// <summary>
		/// Average True Range Moving Average
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <param name="atrPeriod"></param>
		/// <param name="maPeriod"></param>
		/// <param name="startIndex"></param>
		/// <returns></returns>
		public static double?[] Atrma(double[] high, double[] low, double[] close, int atrPeriod, int maPeriod, int startIndex = 0)
		{
			var atr = Atr(high, low, close, atrPeriod, startIndex);
			return Wma(atr, maPeriod);
		}

		/// <summary>
		/// Relative Strength Index
		/// </summary>
		/// <param name="values"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Rsi(double[] values, int period)
		{
			var rsi = new double?[values.Length];
			var u = new double?[values.Length];
			var d = new double?[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				if (i == 0)
				{
					u[i] = null;
					d[i] = null;
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
		public static double?[] Stoch(double?[] high, double?[] low, double?[] close, int period, int startIndex = 0)
		{
			var stoch = new double?[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i < period - 1 + startIndex)
				{
					stoch[i] = null;
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
		public static (double?[], double?[]) Supertrend(double[] high, double[] low, double[] close, double factor, int atrPeriod)
		{
			var upperBand = new double?[high.Length];
			var lowerBand = new double?[high.Length];
			var supertrend = new double?[high.Length];
			var direction = new double?[high.Length];

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

		public static (double?[], double?[]) ReverseSupertrend(double[] high, double[] low, double[] close, double factor, int atrPeriod)
		{
			var upperBand = new double?[high.Length];
			var lowerBand = new double?[high.Length];
			var supertrend = new double?[high.Length];
			var direction = new double?[high.Length];

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

		public static (double?[], double?[], double?[], double?[], double?[], double?[]) TripleSupertrend(double[] high, double[] low, double[] close, int atrPeriod1, double factor1, int atrPeriod2, double factor2, int atrPeriod3, double factor3)
		{
			(var supertrend1, var direction1) = Supertrend(high, low, close, factor1, atrPeriod1);
			(var supertrend2, var direction2) = Supertrend(high, low, close, factor2, atrPeriod2);
			(var supertrend3, var direction3) = Supertrend(high, low, close, factor3, atrPeriod3);

			return (supertrend1, direction1, supertrend2, direction2, supertrend3, direction3);
		}

		public static (double?[], double?[]) StochasticRsi(double[] close, int smoothK, int smoothD, int rsiPeriod, int stochasticPeriod)
		{
			var rsi = Rsi(close, rsiPeriod);
			var stoch = Stoch(rsi, rsi, rsi, stochasticPeriod, 2);
			var k = Sma(stoch, smoothK);
			var d = Sma(k, smoothD);

			return (k, d);
		}

		public static double?[] Adx(double[] high, double[] low, double[] close, int adxPeriod, int diPeriod)
		{
			var nHigh = high.ToNullable();
			var nLow = low.ToNullable();

			var adx = new double?[high.Length];
			var up = new double?[high.Length];
			var down = new double?[high.Length];
			var plusDm = new double?[high.Length];
			var minusDm = new double?[high.Length];
			var trueRange = new double?[high.Length];
			var _plus = new double?[high.Length];
			var _minus = new double?[high.Length];
			var diff2 = new double?[high.Length];

			up = Change(nHigh);
			down = [.. Change(nLow).Select(x => -x)];
			for (int i = 0; i < high.Length; i++)
			{
				if (i == 0)
				{
					plusDm[i] = null;
					minusDm[i] = null;
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

		public static double?[] Zlema(double?[] values, int period)
		{
			var zlema = new double?[values.Length];

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
		public static double?[] Imacd(double[] high, double[] low, double[] close, int period)
		{
			var result = new double?[high.Length];
			var hlc = new double?[high.Length];

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

		public static (double?[], double?[], double?[], double?[]) Custom(double[] open, double[] high, double[] low, double[] close, double[] volume, int period)
		{
			double alpha = 2.0 / (period + 1);
			var upper = new double?[open.Length];
			var lower = new double?[open.Length];
			var pioneer = new double?[open.Length];
			var player = new double?[open.Length];
			for (int i = 0; i < open.Length; i++)
			{
				if (i < period - 1)
				{
					upper[i] = null;
					lower[i] = null;
					pioneer[i] = null;
					player[i] = null;
					continue;
				}

				if (i == period - 1)
				{
					double sumOpen = 0;
					double sumHigh = 0;
					double sumLow = 0;
					double sumClose = 0;
					double sumVolume = 0;
					for (int j = i - period + 1; j <= i; j++)
					{
						sumOpen += open[j] * volume[j];
						sumHigh += high[j] * volume[j];
						sumLow += low[j] * volume[j];
						sumClose += close[j] * volume[j];
						sumVolume += volume[j];
					}
					upper[i] = sumHigh / sumVolume;
					lower[i] = sumLow / sumVolume;
					pioneer[i] = sumOpen / sumVolume;
					player[i] = sumClose / sumVolume;
					continue;
				}

				upper[i] = alpha * high[i] + (1 - alpha) * upper[i - 1];
				lower[i] = alpha * low[i] + (1 - alpha) * lower[i - 1];
				pioneer[i] = alpha * open[i] + (1 - alpha) * pioneer[i - 1];
				player[i] = alpha * close[i] + (1 - alpha) * player[i - 1];
			}
			return (upper, lower, pioneer, player);
		}

		public static (double?[], double?[], double?[]) TrendRider(double[] high, double[] low, double[] close, int atrPeriod, double atrMultiplier, int rsiPeriod, int macdFastPeriod, int macdSlowPeriod, int macdSignalPeriod)
		{
			var nClose = close.ToNullable();

			var up = new double?[high.Length];
			var dn = new double?[high.Length];
			var di = new double?[high.Length];
			var trend = new double?[high.Length];
			var supertrend = new double?[high.Length];
			var supertrendDirection = new double?[high.Length];

			var atr = Atr(high, low, close, atrPeriod);
			var rsi = Rsi(close, rsiPeriod);
			var macd = Macd(nClose, macdFastPeriod, macdSlowPeriod, macdSignalPeriod).Item1;

			for (int i = 0; i < high.Length; i++)
			{
				if (i < 1)
				{
					up[i] = null;
					dn[i] = null;
					di[i] = null;
					trend[i] = null;
					supertrend[i] = null;
					supertrendDirection[i] = null;
					continue;
				}

				double mid = (high[i] + low[i]) / 2;
				up[i] = mid + atrMultiplier * atr[i];
				dn[i] = mid - atrMultiplier * atr[i];
				di[i] = (close[i] > up[i - 1]) ? 1 : (close[i] < dn[i - 1]) ? -1 : di[i - 1];

				if (di[i] == null)
				{
					trend[i] = trend[i - 1];
					supertrend[i] = dn[i] = Math.Max(dn[i] ?? 0, dn[i - 1] ?? 0);
					supertrendDirection[i] = supertrendDirection[i - 1];
				}
				else if (di[i] > 0) // up trend
				{
					trend[i] = (rsi[i] > 50 && macd[i] > 0) ? -1 : 0;
					supertrend[i] = dn[i] = Math.Max(dn[i] ?? 0, dn[i - 1] ?? 0);
					supertrendDirection[i] = -1;
				}
				else if (di[i] < 0) // down trend
				{
					trend[i] = (rsi[i] < 50 && macd[i] < 0) ? 1 : 0;
					supertrend[i] = up[i] = Math.Min(up[i] ?? 0, up[i - 1] ?? 0);
					supertrendDirection[i] = 1;
				}
			}

			return (trend, supertrend, supertrendDirection);
		}

		public static (double?[], double?[], double?[], double?[], double?[]) PredictiveRanges(double[] high, double[] low, double[] close, int period, double factor)
		{
			var atr = Atr(high, low, close, period).Select(x => x * factor).ToArray();
			var avg = new double?[high.Length];
			var holdAtr = new double?[high.Length];
			var u2 = new double?[high.Length];
			var u = new double?[high.Length];
			var l = new double?[high.Length];
			var l2 = new double?[high.Length];

			avg[0] = close[0];
			holdAtr[0] = 0;
			u2[0] = avg[0];
			u[0] = avg[0];
			l[0] = avg[0];
			l2[0] = avg[0];
			for (int i = 1; i < high.Length; i++)
			{
				avg[i] = close[i] - avg[i - 1] > atr[i] ? avg[i - 1] + atr[i] :
					(avg[i - 1] - close[i] > atr[i] ? avg[i - 1] - atr[i] :
					avg[i - 1]);

				holdAtr[i] = avg[i] != avg[i - 1] ? atr[i] / 2 : holdAtr[i - 1];

				u2[i] = avg[i] + holdAtr[i] * 2;
				u[i] = avg[i] + holdAtr[i];
				l[i] = avg[i] - holdAtr[i];
				l2[i] = avg[i] - holdAtr[i] * 2;
			}

			return (u2, u, avg, l, l2);
		}

		/// <summary>
		/// Machine Learning of Momentum Index
		/// </summary>
		/// <param name="predictionDataPeriod"></param>
		/// <param name="trendLength"></param>
		/// <returns></returns>
		public static (double?[], double?[]) Mlmi(double[] close, int predictionDataPeriod, int trendLength)
		{
			var nClose = close.ToNullable();
			var quickMa = Wma(nClose, 5);
			var slowMa = Wma(nClose, 20);
			var quickRsi = Wma(Rsi(close, 5), trendLength);
			var slowRsi = Wma(Rsi(close, 20), trendLength);

			var positive = Crossover(quickMa, slowMa);
			var negative = Crossunder(quickMa, slowMa);

			var data = new KnnPredictionData2() { Parameter1 = [0], Parameter2 = [0], PriceArray = [0], ResultArray = [0] };
			var prediction = new double?[close.Length];

			for (int i = 0; i < close.Length; i++)
			{
				if ((positive[i] ?? false) || (negative[i] ?? false))
				{
					data.StorePreviousTrade(slowRsi[i], quickRsi[i], close[i]);
				}

				prediction[i] = data.KnnPredict(slowRsi[i], quickRsi[i], predictionDataPeriod);
			}

			var predictionMa = Wma(prediction, 20);

			return (prediction, predictionMa);
		}

		/// <summary>
		/// Machine Learning of Momentum Index with Pivot
		/// </summary>
		/// <param name="close"></param>
		/// <param name="pivotBars"></param>
		/// <param name="momentumWindow"></param>
		/// <param name="maxData"></param>
		/// <param name="numNeighbors"></param>
		/// <param name="predictionSmoothing"></param>
		/// <returns></returns>
		public static (double?[], double?[]) Mlmip(double[] high, double[] low, double[] close, int pivotBars, int momentumWindow, int maxData, int numNeighbors, int predictionSmoothing)
		{
			//var nClose = close.ToNullable();
			var parameter1 = Wma(Rsi(close, 12), momentumWindow);
			var parameter2 = Wma(Rsi(close, 25), momentumWindow);
			var parameter3 = Wma(Rsi(close, 50), momentumWindow);
			var parameter4 = Wma(Rsi(close, 100), momentumWindow);

			var data = new KnnPredictionData4()
			{
				Parameter1 = [.. Enumerable.Repeat<double?>(null, numNeighbors)],
				Parameter2 = [.. Enumerable.Repeat<double?>(null, numNeighbors)],
				Parameter3 = [.. Enumerable.Repeat<double?>(null, numNeighbors)],
				Parameter4 = [.. Enumerable.Repeat<double?>(null, numNeighbors)],
				PriceArray = [.. Enumerable.Repeat<double?>(null, numNeighbors)],
				ResultArray = [.. Enumerable.Repeat<double?>(null, numNeighbors)]
			};
			(var phDetected, var plDetected) = data.DetectPivots(high, low, pivotBars);
			var prediction = new double?[close.Length];

			for (int i = 0; i < close.Length; i++)
			{
				if (phDetected[i] || plDetected[i])
				{
					data.StorePreviousTrade(parameter1[i], parameter2[i], parameter3[i], parameter4[i], close[i], maxData);
				}
				prediction[i] = data.KnnPredict(parameter1[i], parameter2[i], parameter3[i], parameter4[i], numNeighbors);
			}

			prediction = data.Rescale(prediction, prediction.Min(), prediction.Max(), -100, 100);
			//var predictionStdevMa = Ema(Stdev(prediction, 20), 20);

			//double?[] lowline = new double?[predictionStdevMa.Length];
			//double?[] highline = new double?[predictionStdevMa.Length];
			//for (int i = 0; i < predictionStdevMa.Length; i++)
			//{
			//	lowline[i] = predictionStdevMa[i] == null ? null : predictionStdevMa[i] - 100;
			//	highline[i] = predictionStdevMa[i] == null ? null : 100 - predictionStdevMa[i];
			//}

			var predictionMa = Wma(prediction, predictionSmoothing);

			return (prediction, predictionMa);
		}

		/// <summary>
		/// Donchian Channel
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static (double?[], double?[], double?[]) DonchianChannel(double[] high, double[] low, int period)
		{
			var upper = Highest(high, period);
			var lower = Lowest(low, period);
			var basis = Average(upper, lower);

			return (basis, upper, lower);
		}

		/// <summary>
		/// Commodity Channel Index (TODO)
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		public static double?[] Cci(double[] high, double[] low, double[] close, int period)
		{
			var result = new double?[close.Length];

			for (int i = 0; i < close.Length; i++)
			{
				if (i < period - 1)
				{
					result[i] = null;
					continue;
				}

				var typicalPrices = new double[period];
				for (int j = 0; j < period; j++)
				{
					var idx = i - period + 1 + j;
					var h = high[idx];
					var l = low[idx];
					var c = close[idx];
					typicalPrices[j] = (h + l + c) / 3.0;
				}

				var tp = typicalPrices[period - 1];
				var sma = typicalPrices.Average();
				var meanDev = typicalPrices.Average(x => Math.Abs(x - sma));

				if (meanDev == 0)
					result[i] = 0;
				else
					result[i] = (tp - sma) / (0.015 * meanDev);
			}

			return result;
		}


		/// <summary>
		/// on test
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		public static double?[] MarketScore(double[] high, double[] low, double[] close)
		{
			var result = new double?[high.Length];
			var nHigh = high.ToNullable();
			var nLow = low.ToNullable();
			var nClose = close.ToNullable();

			var adx = Adx(high, low, close, 14, 14);
			var shortEma = Ema(nClose, 20);
			var longEma = Ema(nClose, 60);

			var rsi = Rsi(close, 14);
			var stoch = Stoch(nHigh, nLow, nClose, 14);

			int crossCount = 0;

			result[0] = null;
			for (int i = 1; i < high.Length; i++)
			{
				if (shortEma[i] == null || longEma[i] == null || adx[i] == null || rsi[i] == null || stoch[i] == null)
				{
					result[i] = null;
					continue;
				}

				bool isTrend = adx[i] >= 25;
				bool isBull = shortEma[i] > longEma[i];
				bool isBear = shortEma[i] < longEma[i];

				if ((isBull && shortEma[i - 1] <= longEma[i - 1]) || (isBear && shortEma[i - 1] >= longEma[i - 1]))
				{
					crossCount++;
				}

				if (i >= 20)
				{
					crossCount = 0;
					for (int j = i - 20; j < i; j++)
					{
						if ((shortEma[j] > longEma[j] && shortEma[j - 1] <= longEma[j - 1]) ||
							(shortEma[j] < longEma[j] && shortEma[j - 1] >= longEma[j - 1]))
						{
							crossCount++;
						}
					}
				}

				bool isOverbought = rsi[i] > 70;
				bool isOversold = rsi[i] < 30;
				bool isStochasticOverbought = stoch[i] > 80;
				bool isStochasticOversold = stoch[i] < 20;

				if (adx[i] < 25 && crossCount > 3)
				{
					result[i] = 0.0;
				}
				else
				{
					result[i] =
						isBull && isTrend && !isOverbought && !isStochasticOverbought ? 1.0 :
						isBear && isTrend && !isOversold && !isStochasticOversold ? -1.0 :
						0.0;
				}
			}

			return result;
		}

		/// <summary>
		/// Squeeze Momentum Indicator
		/// </summary>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <param name="bbPeriod"></param>
		/// <param name="bbFactor"></param>
		/// <param name="kcPeriod"></param>
		/// <param name="kcFactor"></param>
		/// <param name="useTrueRange"></param>
		/// <returns></returns>
		public static (double?[], int?[], int?[]) SqueezeMomentum(double[] high, double[] low, double[] close, int bbPeriod, double bbFactor, int kcPeriod, double kcFactor, bool useTrueRange)
		{
			var nHigh = high.ToNullable();
			var nLow = low.ToNullable();
			var nClose = close.ToNullable();
			var basis = Sma(nClose, bbPeriod);
			var dev = Multiply(Stdev(nClose, kcPeriod), kcFactor);
			var upperBb = Add(basis, dev);
			var lowerBb = Subtract(basis, dev);

			var ma = Sma(nClose, kcPeriod);
			var range = useTrueRange ? Tr(high, low, close).ToNullable() : Subtract(nHigh, nLow);
			range[0] = null; // 예외
			var rangeMa = Sma(range, kcPeriod);
			var upperKc = Add(ma, Multiply(rangeMa, kcFactor));
			var lowerKc = Subtract(ma, Multiply(rangeMa, kcFactor));

			var sqzOn = And(GreaterThan(lowerBb, lowerKc), LessThan(upperBb, upperKc));
			var sqzOff = And(LessThan(lowerBb, lowerKc), GreaterThan(upperBb, upperKc));
			var noSqz = Not(Or(sqzOn, sqzOff));

			var avg = Subtract(nClose, Average(Average(Highest(high, kcPeriod), Lowest(low, kcPeriod)), Sma(nClose, kcPeriod)));

			var value = new double?[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i < kcPeriod - 1)
				{
					value[i] = null;
					continue;
				}
				value[i] = LinearRegression(avg, kcPeriod, i);
			}

			var bColor = new int?[high.Length];
			var sColor = new int?[high.Length];

			for (int i = 0; i < high.Length; i++)
			{
				if (i == 0 || value[i] == null || value[i - 1] == null)
				{
					bColor[i] = null;
				}
				else
				{
					var value0 = value[i] ?? 0;
					var value1 = value[i - 1] ?? 0;

					bColor[i] = value0 > 0
					? (value0 > value1 ? 1 : 2)
					: (value0 < value1 ? -1 : -2);
				}

				sColor[i] = sqzOn[i] == null ? null : (noSqz[i] ?? false ? 0 : (sqzOn[i] ?? false ? 1 : 2));
			}

			return (value, bColor, sColor);
		}

		/// <summary>
		/// Exponential Weighted Moving Average Crossover
		/// </summary>
		/// <param name="values"></param>
		/// <param name="shortPeriod"></param>
		/// <param name="longPeriod"></param>
		/// <param name="startIndex"></param>
		/// <returns></returns>
		public static double?[] Ewmac(double?[] values, int shortPeriod, int longPeriod, int startIndex = 0)
		{
			var shortEma = Ema(values, shortPeriod, startIndex);
			var longEma = Ema(values, longPeriod, startIndex);
			var result = new double?[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				if (shortEma[i] == null || longEma[i] == null)
				{
					result[i] = null;
					continue;
				}

				result[i] = shortEma[i] - longEma[i];
			}

			return result;
		}

		/// <summary>
		/// Volatility Ratio
		/// </summary>
		/// <param name="values"></param>
		/// <param name="currentPeriod"></param>
		/// <param name="longPeriod"></param>
		/// <returns></returns>
		public static double?[] VolatilityRatio(double?[] values, int currentPeriod, int longPeriod)
		{
			var currentStdev = Stdev(values, currentPeriod);
			var longTermStdev = Stdev(values, longPeriod);
			var result = new double?[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				if (currentStdev[i] == null || longTermStdev[i] == null || longTermStdev[i] == 0)
				{
					result[i] = null;
					continue;
				}

				result[i] = currentStdev[i] / longTermStdev[i];
			}

			return result;
		}

		/// <summary>
		/// Candle Score
		/// </summary>
		/// <param name="open"></param>
		/// <param name="high"></param>
		/// <param name="low"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		public static double?[] CandleScore(double[] open, double[] high, double[] low, double[] close, double[] volume, int window = 5)
		{
			var score = new double?[close.Length];
			var ma50 = Sma(close.ToNullable(), 50);  // 50-period 이동 평균
			var rsi = Rsi(close, 14);    // 14-period RSI
			var macd = Macd(close.ToNullable(), 12, 26, 9);  // 12, 26, 9 MACD

			for (int i = window; i < close.Length; i++)
			{
				double s = 0;

				// 캔들 정보
				double o = open[i];
				double h = high[i];
				double l = low[i];
				double c = close[i];
				double prevC = close[i - 1];
				double prevO = open[i - 1];

				double body = Math.Abs(c - o);
				double candleSize = h - l;
				double upperTail = h - Math.Max(c, o);
				double lowerTail = Math.Min(c, o) - l;

				// 1. 몸통 비율
				if (c > o && body / candleSize > 0.6) s += 1.5;
				else if (c < o && body / candleSize > 0.6) s -= 1.5;

				// 2. 갭 상승/하락
				if (o > prevC * 1.002) s += 0.5;
				else if (o < prevC * 0.998) s -= 0.5;

				// 3. 꼬리 길이로 반전 판단
				if (c > o && lowerTail > body * 1.5) s += 0.5;
				else if (c < o && upperTail > body * 1.5) s -= 0.5;

				// 4. 되돌림 캔들
				bool reversalBull = c > prevO && o < prevC;
				bool reversalBear = c < prevO && o > prevC;
				if (reversalBull) s += 1.0;
				else if (reversalBear) s -= 1.0;

				// 5. 최근 window 내 연속 양봉/음봉
				int bullCount = 0;
				int bearCount = 0;
				for (int j = i - window + 1; j <= i; j++)
				{
					if (close[j] > open[j]) bullCount++;
					else if (close[j] < open[j]) bearCount++;
				}
				if (bullCount >= window - 1) s += 1.5;
				else if (bearCount >= window - 1) s -= 1.5;

				// 6. 스파이크 캔들 (size가 최근 평균보다 크면)
				double avgSize = 0;
				for (int j = i - window; j < i; j++)
				{
					avgSize += high[j] - low[j];
				}
				avgSize /= window;

				if (candleSize > avgSize * 1.8)
					s += (c > o ? 1.0 : -1.0);

				// 7. 이동평균에 따른 추세 판단 (50-period MA)
				if (ma50[i] > ma50[i - 1])  // 상승 추세
					s += 0.5;
				else if (ma50[i] < ma50[i - 1])  // 하락 추세
					s -= 0.5;

				// 8. RSI를 통한 진입 필터 (과매도/과매수)
				if (rsi[i] < 30) s += 0.5;  // 과매도
				else if (rsi[i] > 70) s -= 0.5;  // 과매수

				// 9. MACD 신호 (MACD 히스토그램이 0 이상일 때 롱, 이하일 때 숏)
				if (macd.Item3[i] > 0) s += 0.5;  // 롱
				else if (macd.Item3[i] < 0) s -= 0.5;  // 숏

				// 스코어 계산
				score[i] = Math.Round(s, 2);
			}

			return score;
		}



	}
}
