using System;

namespace TradeBot.Models
{
    public class SymbolDetail
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime ListingDate { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal TickSize { get; set; }
        public decimal MaxQuantity { get; set; }
        public decimal MinQuantity { get; set; }
        public decimal StepSize { get; set; }
        public int PricePrecision { get; set; }
        public int QuantityPrecision { get; set; }
        public int PriceTick => TickSize switch
        {
            1m => 0,
            0.1m => 1,
            0.01m => 2,
            0.001m => 3,
            0.0001m => 4,
            0.00001m => 5,
            0.000001m => 6,
            0.0000001m => 7,
            0.00000001m => 8,
            0.000000001m => 9,
            _ => 10
        };
        public int QuantityTick => MinQuantity switch
        {
            1m => 0,
            0.1m => 1,
            0.01m => 2,
            0.001m => 3,
            0.0001m => 4,
            0.00001m => 5,
            0.000001m => 6,
            0.0000001m => 7,
            0.00000001m => 8,
            0.000000001m => 9,
            _ => 10
        };
    }
}
