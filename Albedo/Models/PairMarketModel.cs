using Albedo.Enums;

namespace Albedo.Models
{
    public class PairMarketModel
    {
        public PairMarket PairMarket { get; set; }
        public string Text { get; set; }
        public string Icon { get; set; }

        public PairMarketModel(PairMarket pairMarket, string text, string icon)
        {
            PairMarket = pairMarket;
            Text = text;
            Icon = icon;
        }
    }
}
