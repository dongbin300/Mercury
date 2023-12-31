﻿using MercuryTradingModel.Enums;
using MercuryTradingModel.Interfaces;

using Newtonsoft.Json;

using static MercuryTradingModel.Indicators.IndicatorBaseValue;

namespace MercuryTradingModel.Elements
{
    public class ChartElement : IElement
    {
        public ChartElementType ElementType { get; set; } = ChartElementType.None;
        public decimal[] Parameters { get; set; } = new decimal[4];
        [JsonIgnore]
        public bool IsBaseElement { get; set; }

        public ChartElement()
        {

        }

        public ChartElement(ChartElementType element, decimal[] parameters)
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
                ElementType = (ChartElementType)Enum.Parse(typeof(ChartElementType), segments[0]);

                // base element
                if (segments.Length == 1)
                {
                    IsBaseElement = true;
                    switch (ElementType)
                    {
                        case ChartElementType.ma:
                        case ChartElementType.ema:
                            Parameters[0] = MaPeriod;
                            return;

                        case ChartElementType.ri:
                        case ChartElementType.rsi:
                            Parameters[0] = RsiPeriod;
                            return;

                        case ChartElementType.bb_sma:
                        case ChartElementType.bb_upper:
                        case ChartElementType.bb_lower:
                            Parameters[0] = BollingerBandsPeriod;
                            Parameters[1] = BollingerBandsStandardDeviation;
                            return;

                        case ChartElementType.macd_macd:
                        case ChartElementType.macd_signal:
                        case ChartElementType.macd_hist:
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
                    case ChartElementType.ma:
                    case ChartElementType.ema:
                    case ChartElementType.ri:
                    case ChartElementType.rsi:
                        Parameters[0] = decimal.Parse(segments[1]);
                        return;

                    case ChartElementType.bb_sma:
                    case ChartElementType.bb_upper:
                    case ChartElementType.bb_lower:
                        ElementType = (ChartElementType)Enum.Parse(typeof(ChartElementType), segments[0]);
                        Parameters[0] = decimal.Parse(segments[1]);
                        Parameters[1] = decimal.Parse(segments[2]);
                        return;

                    case ChartElementType.macd_macd:
                    case ChartElementType.macd_signal:
                    case ChartElementType.macd_hist:
                        ElementType = (ChartElementType)Enum.Parse(typeof(ChartElementType), segments[0]);
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
