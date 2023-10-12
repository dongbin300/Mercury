﻿using MercuryTradingModel.Interfaces;

namespace MercuryTradingModel.Elements
{
    public class RangeElement : IElement
    {
        public decimal StartValue { get; set; }
        public decimal EndValue { get; set; }

        public RangeElement(decimal startValue, decimal endValue)
        {
            StartValue = startValue;
            EndValue = endValue;
        }

        public bool IsValid(decimal num)
        {
            return num >= StartValue && num <= EndValue;
        }

        public override string ToString()
        {
            return StartValue.ToString() + "~" + EndValue.ToString();
        }
    }
}
