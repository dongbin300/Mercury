using Albedo.Enums;

namespace Albedo.Models
{
    public class PairQuoteAssetModel
    {
        public PairQuoteAsset PairQuoteAsset { get; set; }
        public string Text { get; set; }

        public PairQuoteAssetModel(PairQuoteAsset pairQuoteAsset, string text)
        {
            PairQuoteAsset = pairQuoteAsset;
            Text = text;
        }
    }
}
