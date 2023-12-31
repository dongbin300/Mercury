﻿using MercuryEditor.Enums;

namespace MercuryEditor.Inspection.SystemCode
{
    internal class MercurySystemCodeFormat
    {
        public int CodeVersion { get; set; } = 0;
        public MarketPlatform MarketPlatform { get; set; } = MarketPlatform.None;
        public ModelType ModelType { get; set; } = ModelType.None;
    }
}
