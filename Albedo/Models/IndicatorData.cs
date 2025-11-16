using System;

namespace Albedo.Models
{
    public class IndicatorData(DateTime date, decimal? value)
	{
		public DateTime Date { get; set; } = date;
		public decimal? Value { get; set; } = value;
	}
}
