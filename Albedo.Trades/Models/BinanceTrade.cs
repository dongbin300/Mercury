namespace Albedo.Trades.Models
{
    public class BinanceTrade
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public bool BuyerIsMaker { get; set; }
        public decimal Amount => Price * Quantity;
        public decimal Highlight { get; set; }
        public bool IsBigHand => Amount > Highlight;

        public BinanceTrade(decimal price, decimal quantity, bool buyerIsMaker)
        {
            Price = price;
            Quantity = quantity;
            BuyerIsMaker = buyerIsMaker;
        }

        public void SetHighlight(decimal value)
        {
            Highlight = value;
        }
    }
}
