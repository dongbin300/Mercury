using Mercury.Enums;
using Mercury.Interfaces;

using Newtonsoft.Json;

using static Mercury.Indicators.MtmIndicatorBaseValue;

namespace Mercury.Elements
{
	public class ChartElement : IElement
	{
		public MtmChartElementType ElementType { get; set; } = MtmChartElementType.None;
		public decimal[] Parameters { get; set; } = new decimal[4];
		[JsonIgnore]
		public bool IsBaseElement { get; set; }

		public ChartElement()
		{

		}

		public ChartElement(MtmChartElementType element, decimal[] parameters)
		{
			ElementType = element;
			Parameters = parameters;
		}

		public ChartElement(string elementString)
		{
			try
			{
				var segments = elementString.Split(',').Select(x => x.Trim()).ToArray();
				segments[0] = segments[0].Replace('.', '_');
				ElementType = Enum.Parse<MtmChartElementType>(segments[0]);

				// base element
				if (segments.Length == 1)
				{
					IsBaseElement = true;
					switch (ElementType)
					{
						case MtmChartElementType.ma:
						case MtmChartElementType.ema:
							Parameters[0] = MaPeriod;
							return;

						case MtmChartElementType.ri:
						case MtmChartElementType.rsi:
							Parameters[0] = RsiPeriod;
							return;

						case MtmChartElementType.bb_sma:
						case MtmChartElementType.bb_upper:
						case MtmChartElementType.bb_lower:
							Parameters[0] = BollingerBandsPeriod;
							Parameters[1] = BollingerBandsStandardDeviation;
							return;

						case MtmChartElementType.macd_macd:
						case MtmChartElementType.macd_signal:
						case MtmChartElementType.macd_hist:
							Parameters[0] = MacdFastPeriod;
							Parameters[1] = MacdSlowPeriod;
							Parameters[2] = MacdSignalPeriod;
							return;

						default:
							IsBaseElement = false;
							return;
					}
				}

				// parametered element
				switch (ElementType)
				{
					case MtmChartElementType.ma:
					case MtmChartElementType.ema:
					case MtmChartElementType.ri:
					case MtmChartElementType.rsi:
						Parameters[0] = decimal.Parse(segments[1]);
						return;

					case MtmChartElementType.bb_sma:
					case MtmChartElementType.bb_upper:
					case MtmChartElementType.bb_lower:
						ElementType = Enum.Parse<MtmChartElementType>(segments[0]);
						Parameters[0] = decimal.Parse(segments[1]);
						Parameters[1] = decimal.Parse(segments[2]);
						return;

					case MtmChartElementType.macd_macd:
					case MtmChartElementType.macd_signal:
					case MtmChartElementType.macd_hist:
						ElementType = Enum.Parse<MtmChartElementType>(segments[0]);
						Parameters[0] = decimal.Parse(segments[1]);
						Parameters[1] = decimal.Parse(segments[2]);
						Parameters[2] = decimal.Parse(segments[3]);
						return;

					default:
						return;
				}
			}
			catch
			{
			}
		}

		public override string ToString()
		{
			var meaningfulParameters = Parameters.Where(x => x != 0);
			return ElementType.ToString() + (meaningfulParameters.Any() ? $"({string.Join(',', meaningfulParameters)})" : "");
		}
	}
}
