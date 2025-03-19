using Mercury.Assets;
using Mercury.Charts;
using Mercury.Elements;
using Mercury.Enums;
using Mercury.Formulae;
using Mercury.Interfaces;

namespace Mercury.Signals
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

		private decimal? GetElementValue(IElement element, Asset asset, ChartInfo chart)
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

		public virtual bool IsFlare(Asset asset, ChartInfo chart, ChartInfo prevChart) => Formula switch
		{
			ComparisonFormula x => IsFlare(x, asset, chart, prevChart),
			CrossFormula x => IsFlare(x, asset, chart, prevChart),
			AndFormula x => IsFlare(x.Formula1, asset, chart, prevChart) && IsFlare(x.Formula2, asset, chart, prevChart),
			OrFormula x => IsFlare(x.Formula1, asset, chart, prevChart) || IsFlare(x.Formula2, asset, chart, prevChart),
			_ => false
		};

		private bool IsFlare(IFormula? formula, Asset asset, ChartInfo chart, ChartInfo prevChart)
		{
			return formula switch
			{
				ComparisonFormula x => x.Comparison switch
				{
					MtmComparison.Equal => GetElementValue(x.Element1, asset, chart) == GetElementValue(x.Element2, asset, chart),
					MtmComparison.NotEqual => GetElementValue(x.Element1, asset, chart) != GetElementValue(x.Element2, asset, chart),
					MtmComparison.LessThan => GetElementValue(x.Element1, asset, chart) < GetElementValue(x.Element2, asset, chart),
					MtmComparison.LessThanOrEqual => GetElementValue(x.Element1, asset, chart) <= GetElementValue(x.Element2, asset, chart),
					MtmComparison.GreaterThan => GetElementValue(x.Element1, asset, chart) > GetElementValue(x.Element2, asset, chart),
					MtmComparison.GreaterThanOrEqual => GetElementValue(x.Element1, asset, chart) >= GetElementValue(x.Element2, asset, chart),
					_ => false
				},
				CrossFormula x => x.Cross switch
				{
					MtmCross.GoldenCross => GetElementValue(x.Element1, asset, prevChart) <= GetElementValue(x.Element2, asset, prevChart) && GetElementValue(x.Element1, asset, chart) >= GetElementValue(x.Element2, asset, chart),
					MtmCross.DeadCross => GetElementValue(x.Element1, asset, prevChart) >= GetElementValue(x.Element2, asset, prevChart) && GetElementValue(x.Element1, asset, chart) <= GetElementValue(x.Element2, asset, chart),
					_ => false
				},
				_ => false
			};
		}
	}
}
