﻿using Mercury.Enums;
using Mercury.Formulae;
using Mercury.Interfaces;
using Mercury.Orders;
using Mercury.TradingModels;

using MercuryEditor.Editor;

using MercuryTradingModel.Assets;
using MercuryTradingModel.Cues;
using MercuryTradingModel.Elements;
using MercuryTradingModel.Extensions;
using MercuryTradingModel.Formulae;
using MercuryTradingModel.Intervals;
using MercuryTradingModel.Signals;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MercuryEditor.Inspection.V1
{
	internal class MercuryBinanceFuturesBackTestInspector
    {
        private int lineNumber = 0;
        public MercuryBackTestTradingModel TradingModel { get; private set; } = new MercuryBackTestTradingModel();

        public MercuryBinanceFuturesBackTestInspector()
        {
        }

        public MercuryBinanceFuturesBackTestInspector(int lineNumber)
        {
            this.lineNumber = lineNumber;
        }

        public string Run(List<TextLine> code)
        {
            try
            {
                var assetResult = ParseAsset(code);
                if (assetResult != string.Empty)
                {
                    return assetResult;
                }

                var periodResult = ParsePeriod(code);
                if (periodResult != string.Empty)
                {
                    return periodResult;
                }

                var intervalResult = ParseInterval(code);
                if (intervalResult != string.Empty)
                {
                    return intervalResult;
                }

                var targetResult = ParseTarget(code);
                if (targetResult != string.Empty)
                {
                    return targetResult;
                }

                var namedElementResult = ParseNamedElement(code);
                if (namedElementResult != string.Empty)
                {
                    return namedElementResult;
                }

                var scenarioResult = ParseScenario(code);
                if (scenarioResult != string.Empty)
                {
                    return scenarioResult;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string ParseAsset(List<TextLine> code)
        {
            TextLine? assetCode = null;
            try
            {
                assetCode = code.Find(x => x.Text.StartsWith("asset"));
                if (assetCode == null)
                {
                    TradingModel.Asset = 100000;
                    return string.Empty;
                }
                lineNumber = assetCode.LineNumber;
                var assetValue = assetCode.Text.Split('=')[1].Trim();
                decimal asset = 0m;
                if (assetValue.EndsWith('k'))
                {
                    var assetValue2 = assetValue[..^1];
                    asset = decimal.Parse(assetValue2) * 1000;
                }
                else if (assetValue.EndsWith('m'))
                {
                    var assetValue2 = assetValue[..^1];
                    asset = decimal.Parse(assetValue2) * 1000000;
                }
                else
                {
                    asset = decimal.Parse(assetValue);
                }
                TradingModel.Asset = asset;
                return string.Empty;
            }
            catch
            {
                return $"{Delegater.CurrentLanguageDictionary["AssetTypeError"]} :: {assetCode}";
            }
        }

        private string ParsePeriod(List<TextLine> code)
        {
            TextLine? periodCode = null;
            try
            {
                periodCode = code.Find(x => x.Text.StartsWith("period"));
                if (periodCode == null)
                {
                    var temp = DateTime.Now.AddDays(-7);
                    TradingModel.StartTime = new DateTime(temp.Year, temp.Month, temp.Day, temp.Hour, 0, 0);
                    TradingModel.Period = new TimeSpan(7, 0, 0, 0);
                    return string.Empty;
                }
                lineNumber = periodCode.LineNumber;
                var periodValue = periodCode.Text.Split('=')[1].Trim();
                var startTimeString = periodValue.Split(':')[0];
                var periodString = periodValue.Split(':')[1];
                var startTimeSegments = startTimeString.Split(',');
                var periodSegments = periodString.Split(',');
                var startTime = new DateTime(
                    int.Parse(startTimeSegments[0]),
                    int.Parse(startTimeSegments[1]),
                    int.Parse(startTimeSegments[2]),
                    int.Parse(startTimeSegments[3]),
                    int.Parse(startTimeSegments[4]),
                    0
                    );
                var period = new TimeSpan(
                    int.Parse(periodSegments[0]) * 365 + int.Parse(periodSegments[1]) * 30 + int.Parse(periodSegments[2]),
                    int.Parse(periodSegments[3]),
                    int.Parse(periodSegments[4]),
                    0
                    );
                TradingModel.StartTime = startTime;
                TradingModel.Period = period;
                return string.Empty;
            }
            catch
            {
                return $"{Delegater.CurrentLanguageDictionary["PeriodTypeError"]} :: {periodCode}";
            }
        }

        private string ParseInterval(List<TextLine> code)
        {
            TextLine? intervalCode = null;
            try
            {
                intervalCode = code.Find(x => x.Text.StartsWith("interval"));
                if (intervalCode == null)
                {
                    TradingModel.Interval = Binance.Net.Enums.KlineInterval.OneMinute;
                    return string.Empty;
                }
                lineNumber = intervalCode.LineNumber;
                var intervalValue = intervalCode.Text.Split('=')[1].Trim();
                var interval = intervalValue.ToKlineInterval();
                TradingModel.Interval = interval;
                return string.Empty;
            }
            catch
            {
                return $"{Delegater.CurrentLanguageDictionary["IntervalTypeError"]} :: {intervalCode}";
            }
        }

        private string ParseTarget(List<TextLine> code)
        {
            TextLine? targetCode = null;
            try
            {
                targetCode = code.Find(x => x.Text.StartsWith("target"));
                if (targetCode == null)
                {
                    TradingModel.Targets = new List<string>() { "BTCUSDT" };
                    return string.Empty;
                }
                lineNumber = targetCode.LineNumber;
                var targetValue = targetCode.Text.Split('=')[1].Trim();
                var target = targetValue.ToUpper();
                TradingModel.Targets.Add(target);
                return string.Empty;
            }
            catch
            {
                return $"{Delegater.CurrentLanguageDictionary["TargetTypeError"]} :: {targetCode}";
            }
        }

        private string ParseNamedElement(List<TextLine> code)
        {
            IList<TextLine>? namedElementCodes = null;
            try
            {
                namedElementCodes = code.FindAll(x => x.Text.Contains('@'));
                if (namedElementCodes == null)
                {
                    return string.Empty;
                }
                foreach (var namedElementCode in namedElementCodes)
                {
                    lineNumber = namedElementCode.LineNumber;
                    var parameterString = namedElementCode.Text.Split('@')[0].Trim();
                    var name = namedElementCode.Text.Split('@')[1].Trim();
                    var result = TradingModel.AddNamedElement(name, parameterString);
                    if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result);
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"{Delegater.CurrentLanguageDictionary["NamedElementTypeError"]} :: {ex.Message}";
            }
        }

        private string ParseScenario(List<TextLine> code)
        {
            TextLine? scenarioCode = null;
            try
            {
                for (int i = lineNumber; i < code.Max(x => x.LineNumber); i++)
                {
                    scenarioCode = code[i];
                    if (string.IsNullOrEmpty(scenarioCode.Text) || !scenarioCode.Text.Contains('='))
                    {
                        continue;
                    }
                    var equalIndex = scenarioCode.Text.IndexOf('=');
                    var key = scenarioCode.Text[..equalIndex].Trim();
                    var value = scenarioCode.Text[(equalIndex + 1)..].Trim();

                    var strategyResult = ParseStrategy(key, value);
                    if (!string.IsNullOrEmpty(strategyResult))
                    {
                        return strategyResult + scenarioCode.ToString();
                    }
                }
                return string.Empty;
            }
            catch
            {
                return $"{Delegater.CurrentLanguageDictionary["ScenarioTypeError"]} :: {scenarioCode}";
            }
        }

        private string ParseStrategy(string key, string value)
        {
            try
            {
                var keySegments = key.Split('.');
                switch (keySegments[2])
                {
                    case "cue":
                        var cue = ParseCue(value);
                        if (cue == null)
                        {
                            return $"{Delegater.CurrentLanguageDictionary["CueTypeError"]} :: ";
                        }
                        TradingModel.AddCue(keySegments[0], keySegments[1], cue);
                        break;

                    case "signal":
                        var formula = ParseFormula(value);
                        if (formula == null)
                        {
                            return $"{Delegater.CurrentLanguageDictionary["SignalFormulaTypeError"]} :: ";
                        }
                        var signal = new Signal(formula);
                        TradingModel.AddSignal(keySegments[0], keySegments[1], signal);
                        break;

                    case "order":
                        var order = ParseOrder(value);
                        if (order == null)
                        {
                            return $"{Delegater.CurrentLanguageDictionary["OrderTypeError"]} :: ";
                        }
                        TradingModel.AddOrder(keySegments[0], keySegments[1], order);
                        break;

                    case "tag":
                        var tag = ParseTag(value);
                        if (tag == null)
                        {
                            return $"{Delegater.CurrentLanguageDictionary["TagTypeError"]} :: ";
                        }
                        TradingModel.AddTag(keySegments[0], keySegments[1], tag);
                        break;

                    default:
                        return $"{Delegater.CurrentLanguageDictionary["ScenarioKeyTypeError"]} :: ";
                }
                return string.Empty;
            }
            catch
            {
                return $"{Delegater.CurrentLanguageDictionary["StrategyTypeError"]} :: ";
            }
        }

        /// <summary>
        /// Formula, Life
        /// rsi>70, 20
        /// </summary>
        /// <param name="cueValue"></param>
        /// <returns></returns>
        private ICue? ParseCue(string cueValue)
        {
            try
            {
                var segments = cueValue.Split(',').Select(x => x.Trim()).ToArray();
                if (segments.Length == 2)
                {
                    var formula = ParseFormula(segments[0]);
                    var life = int.Parse(segments[1]);

                    return formula switch
                    {
                        null => null,
                        _ => new Cue(formula, life)
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// rsi>70 (Comparison Formula)
        /// rsi<30 | rsi>70 (Or Formula)
        /// candle.close>bb.upper & rsi>75 (And Formula)
        /// </summary>
        /// <param name="signalValue"></param>
        /// <returns></returns>
        private IFormula? ParseFormula(string signalValue)
        {
            try
            {
                var logicSegments1 = signalValue.Split('&').Select(x => x.Trim()).ToArray();
                if (logicSegments1.Length == 1)
                {
                    var logicSegments2 = signalValue.Split('|').Select(x => x.Trim()).ToArray();
                    if (logicSegments2.Length == 1)
                    {
                        var crossSegments1 = signalValue.Split("++").Select(x => x.Trim()).ToArray();
                        if(crossSegments1.Length == 1)
                        {
                            var crossSegments2 = signalValue.Split("--").Select(x => x.Trim()).ToArray();
                            if (crossSegments2.Length == 1)
                            {
                                var segments1 = signalValue.SplitKeep(new string[] { ">=", "<=", "!=" }).Select(x => x.Trim()).ToArray();
                                if (segments1.Length == 1)
                                {
                                    var segments2 = signalValue.SplitKeep(new string[] { ">", "<", "=" }).Select(x => x.Trim()).ToArray();
                                    if (segments2.Length == 1)
                                    {
                                        return null;
                                    }
                                    return ParseComparisonFormula(segments2);
                                }
                                return ParseComparisonFormula(segments1);
                            }
                            return ParseDeadCrossFormula(crossSegments2);
                        }
                        return ParseGoldenCrossFormula(crossSegments1);
                    }
                    return new OrFormula(ParseFormula(logicSegments2[0]), ParseFormula(logicSegments2[1]));
                }
                return new AndFormula(ParseFormula(logicSegments1[0]), ParseFormula(logicSegments1[1]));
            }
            catch
            {
                return null;
            }
        }

        private IFormula? ParseComparisonFormula(string[] formulaSegments)
        {
            try
            {
                var element1 = ParseElement(formulaSegments[0]);
                var comparison = FormulaUtil.ToComparison(formulaSegments[1]);
                var element2 = ParseElement(formulaSegments[2]);

                return new ComparisonFormula(element1, comparison, element2);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// candle.close ++ ema,60
        /// ema,60 ++ ema,120
        /// </summary>
        /// <param name="crossSegments"></param>
        /// <returns></returns>
        private IFormula? ParseGoldenCrossFormula(string[] crossSegments)
        {
            try
            {
                var element1 = ParseElement(crossSegments[0]);
                var element2 = ParseElement(crossSegments[1]);

                return new CrossFormula(element1, MtmCross.GoldenCross, element2);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// candle.close -- ema,60
        /// ema,60 -- ema,120
        /// </summary>
        /// <param name="crossSegments"></param>
        /// <returns></returns>
        private IFormula? ParseDeadCrossFormula(string[] crossSegments)
        {
            try
            {
                var element1 = ParseElement(crossSegments[0]);
                var element2 = ParseElement(crossSegments[1]);

                return new CrossFormula(element1, MtmCross.DeadCross, element2);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 3.6 (Value Element)
        /// roe (Trade Element)
        /// 1~3 (Range Element)
        /// rsi,24 @ myrsi (Named Element)
        /// rsi,21 (Chart Element)
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        private IElement ParseElement(string segment)
        {
            try
            {
                var segment2 = segment.Split(',');

                if (decimal.TryParse(segment, out decimal value))
                {
                    return new ValueElement(value);
                }
                else if (Enum.TryParse<MtmTradeElementType>(segment2[0], out var tradeElementType))
                {
                    return new TradeElement(tradeElementType, decimal.Parse(segment2[1]));
                }
                else if(segment.Contains('~'))
                {
                    var content = segment.Split('~');
                    return new RangeElement(decimal.Parse(content[0]), decimal.Parse(content[1]));
                }
                else if (TradingModel.AnyNamedElement(segment))
                {
                    return TradingModel.GetNamedElement(segment) ?? default!;
                }
                else
                {
                    var chartElement = new ChartElement(segment);
                    if (chartElement.IsBaseElement)
                    {
                        TradingModel.ChartElements.Add(chartElement);
                    }
                    return chartElement;
                }
            }
            catch
            {
                return default!;
            }
        }

        /// <summary>
        /// long, 1, market (Fixed Symbol)
        /// long, 10t, market (Fixed (Tether))
        /// long, seed, 0.1, market (Seed)
        /// long, balance, 0.1, market (Balance)
        /// long, asset, 0.1, market (Asset)
        /// long, balancesymbol, 0.1, market (Balance Symbol)
        /// long, 20, limit, 42.50 (Limit(only fixed symbol))
        /// open, 0.1 (Open)
        /// close, 0.1 (Close)
        /// </summary>
        /// <param name="orderValue"></param>
        /// <returns></returns>
        private BacktestOrder? ParseOrder(string orderValue)
        {
            try
            {
                var segments = orderValue.Split(',').Select(x => x.Trim()).ToArray();
                if (segments.Length < 2)
                {
                    return null;
                }

                var positionSide = segments[0] switch
                {
                    "long" => MtmPositionSide.Long,
                    "short" => MtmPositionSide.Short,
                    "open" => MtmPositionSide.Open,
                    "close" => MtmPositionSide.Close,
                    _ => MtmPositionSide.None
                };

                if (positionSide == MtmPositionSide.Open || positionSide == MtmPositionSide.Close)
                {
                    var _value = decimal.Parse(segments[1]);
                    return new BackTestOrder(MtmOrderType.Market, positionSide, new OrderAmount(MtmOrderAmountType.None, _value));
                }

                MtmOrderType orderType = MtmOrderType.None;
                MtmOrderAmountType orderAmountType = MtmOrderAmountType.None;
                decimal value = 0m;
                decimal? price = null;

                // Fixed Symbol
                if (decimal.TryParse(segments[1], out decimal d1))
                {
                    orderAmountType = MtmOrderAmountType.FixedSymbol;
                    value = d1;

                    orderType = segments[2] switch
                    {
                        "market" => MtmOrderType.Market,
                        "limit" => MtmOrderType.Limit,
                        _ => MtmOrderType.None
                    };
                    if (orderType == MtmOrderType.Limit)
                    {
                        price = decimal.Parse(segments[3]);
                    }
                }
                // Fixed
                else if (decimal.TryParse(segments[1].Replace("t", ""), out decimal d2))
                {
                    orderAmountType = MtmOrderAmountType.Fixed;
                    value = d2;

                    orderType = segments[2] switch
                    {
                        "market" => MtmOrderType.Market,
                        _ => MtmOrderType.None
                    };
                }
                else
                {
                    switch (segments[1])
                    {
                        case "seed":
                            orderAmountType = MtmOrderAmountType.Seed;
                            value = decimal.Parse(segments[2]);
                            break;

                        case "balance":
                            orderAmountType = MtmOrderAmountType.Balance;
                            value = decimal.Parse(segments[2]);
                            break;

                        case "asset":
                            orderAmountType = MtmOrderAmountType.Asset;
                            value = decimal.Parse(segments[2]);
                            break;

                        case "balancesymbol":
                            orderAmountType = MtmOrderAmountType.BalanceSymbol;
                            value = decimal.Parse(segments[2]);
                            break;
                    }

                    orderType = segments[3] switch
                    {
                        "market" => MtmOrderType.Market,
                        _ => MtmOrderType.None
                    };
                }

                var orderAmount = new OrderAmount(orderAmountType, value);
                return new BackTestOrder(orderType, positionSide, orderAmount, price);
            }
            catch
            {
                return null;
            }
        }

        private string? ParseTag(string tagValue)
        {
            return tagValue;
        }
    }
}
