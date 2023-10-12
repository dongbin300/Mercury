using MercuryTradingModel.Assets;
using MercuryTradingModel.Charts;
using MercuryTradingModel.Elements;
using MercuryTradingModel.Enums;
using MercuryTradingModel.Formulae;
using MercuryTradingModel.Interfaces;

namespace MercuryTradingModel.Signals
{
    public class Signal : ISignal
    {
        public IFormula Formula { get; set; } = new Formula();

        public Signal()
        {

        }

        public Signal(IFormula formula)
        {
            Formula = formula;
        }

        private decimal? GetElementValue(IElement element, Asset asset, MercuryChartInfo chart)
        {
            return element switch
            {
                ChartElement x => chart.GetChartElementValue(x.ElementType),
                NamedElement x => chart.GetNamedElementValue(x.Name),
                ValueElement x => x.Value,
                TradeElement x => chart.GetTradeElementValue(asset, x) == 0 ? null : chart.GetTradeElementValue(asset, x),
                _ => null
            };
        }

        public virtual bool IsFlare(Asset asset, MercuryChartInfo chart, MercuryChartInfo prevChart) => Formula switch
        {
            ComparisonFormula x => IsFlare(x, asset, chart, prevChart),
            CrossFormula x => IsFlare(x, asset, chart, prevChart),
            AndFormula x => IsFlare(x.Formula1, asset, chart, prevChart) && IsFlare(x.Formula2, asset, chart, prevChart),
            OrFormula x => IsFlare(x.Formula1, asset, chart, prevChart) || IsFlare(x.Formula2, asset, chart, prevChart),
            _ => false
        };

        private bool IsFlare(IFormula? formula, Asset asset, MercuryChartInfo chart, MercuryChartInfo prevChart)
        {
            return formula switch
            {
                ComparisonFormula x => x.Comparison switch
                {
                    Comparison.Equal => GetElementValue(x.Element1, asset, chart) == GetElementValue(x.Element2, asset, chart),
                    Comparison.NotEqual => GetElementValue(x.Element1, asset, chart) != GetElementValue(x.Element2, asset, chart),
                    Comparison.LessThan => GetElementValue(x.Element1, asset, chart) < GetElementValue(x.Element2, asset, chart),
                    Comparison.LessThanOrEqual => GetElementValue(x.Element1, asset, chart) <= GetElementValue(x.Element2, asset, chart),
                    Comparison.GreaterThan => GetElementValue(x.Element1, asset, chart) > GetElementValue(x.Element2, asset, chart),
                    Comparison.GreaterThanOrEqual => GetElementValue(x.Element1, asset, chart) >= GetElementValue(x.Element2, asset, chart),
                    _ => false
                },
                CrossFormula x => x.Cross switch
                {
                    Cross.GoldenCross => GetElementValue(x.Element1, asset, prevChart) <= GetElementValue(x.Element2, asset, prevChart) && GetElementValue(x.Element1, asset, chart) >= GetElementValue(x.Element2, asset, chart),
                    Cross.DeadCross => GetElementValue(x.Element1, asset, prevChart) >= GetElementValue(x.Element2, asset, prevChart) && GetElementValue(x.Element1, asset, chart) <= GetElementValue(x.Element2, asset, chart),
                    _ => false
                },
                _ => false
            };
        }
    }
}
