using Albedo.Enums;
using Albedo.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Albedo.Extensions
{
    public static class IndicatorExtension
    {
        public static float ToStrokeWidth(this LineWeight lineWeight)
        {
            return (int)lineWeight;
        }

        public static IndicatorData ValueOf(this IEnumerable<IndicatorData> data, int index)
        {
            return data.ElementAtOrDefault(index) ?? new IndicatorData(DateTime.MinValue, 0);
        }
    }
}
