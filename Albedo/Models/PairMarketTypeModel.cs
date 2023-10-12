using Albedo.Enums;

namespace Albedo.Models
{
    public class PairMarketTypeModel
    {
        public PairMarketType PairMarketType { get; set; }
        public string Text { get; set; }

        public PairMarketTypeModel(PairMarketType pairMarketType, string text)
        {
            PairMarketType = pairMarketType;
            Text = text;
        }
    }
}
