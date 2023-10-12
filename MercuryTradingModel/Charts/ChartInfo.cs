using Mercury;

using MercuryTradingModel.Assets;
using MercuryTradingModel.Elements;
using MercuryTradingModel.Enums;

namespace MercuryTradingModel.Charts
{
    public class MercuryChartInfo
    {
        // Basic Info
        public string Symbol { get; set; }
        public string BaseAsset => Symbol.Replace("USDT", "");
        public DateTime DateTime => Quote.Date;
        public Quote Quote { get; set; }

        // Indicator Info
        public IList<ChartElementResult> ChartElements { get; set; } = new List<ChartElementResult>();
        public IList<NamedElementResult> NamedElements { get; set; } = new List<NamedElementResult>();

        public MercuryChartInfo(string symbol, Quote quote)
        {
            Symbol = symbol;
            Quote = quote;
        }

        public override string ToString()
        {
            return $"{Symbol}, {DateTime}, {Quote.Open}:{Quote.High}:{Quote.Low}:{Quote.Close}:{Quote.Volume}, {string.Join(',', ChartElements)}, {string.Join(',', NamedElements)}";
        }

        public ChartElementResult? GetChartElementResult(ChartElementType type) => ChartElements.FirstOrDefault(x => x != null && x.Type.Equals(type), null);
        public decimal? GetChartElementValue(ChartElementType type) => type switch
        {
            ChartElementType.candle_open => Quote.Open,
            ChartElementType.candle_high => Quote.High,
            ChartElementType.candle_low => Quote.Low,
            ChartElementType.candle_close => Quote.Close,
            ChartElementType.volume => Quote.Volume,
            _ => GetChartElementResult(type)?.Value,
        };
        public NamedElementResult? GetNamedElementResult(string name) => NamedElements.FirstOrDefault(x => x != null && x.Name.Equals(name), null);
        public decimal? GetNamedElementValue(string name) => GetNamedElementResult(name)?.Value;
        public decimal? GetTradeElementValue(Asset asset, TradeElement tradeElement) => tradeElement.ElementType switch
        {
            TradeElementType.roe => asset.Position.AveragePrice * (1 + (tradeElement.Value / 100)),
            _ => tradeElement.Value
        };
    }
}
