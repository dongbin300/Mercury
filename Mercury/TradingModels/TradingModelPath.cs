﻿using Mercury.Extensions;

namespace Mercury.TradingModels
{
	public class TradingModelPath
	{
		public static string InspectedDirectory => Environment.CurrentDirectory.Down("Temp");
		public static string InspectedBackTestDirectory => InspectedDirectory.Down("BackTest");
		public static string InspectedMockTradeDirectory => InspectedDirectory.Down("MockTrade");
		public static string InspectedRealTradeDirectory => InspectedDirectory.Down("RealTrade");

		public static void Init()
		{
			InspectedDirectory.TryCreateDirectory();
			InspectedBackTestDirectory.TryCreateDirectory();
			InspectedMockTradeDirectory.TryCreateDirectory();
			InspectedRealTradeDirectory.TryCreateDirectory();
		}
	}
}
