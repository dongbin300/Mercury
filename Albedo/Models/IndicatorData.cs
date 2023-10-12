using System;

namespace Albedo.Models
{
    public class IndicatorData
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }

        public IndicatorData(DateTime date, double value)
        {
            Date = date;
            Value = value;
        }
    }
}
