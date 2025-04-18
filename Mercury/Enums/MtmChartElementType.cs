﻿namespace Mercury.Enums
{
	public enum MtmChartElementType
	{
		None = 0,

		position = 10,

		candle_open = 103,
		candle_high = 109,
		candle_low = 100,
		candle_close = 106,
		volume = 105,

		ri = 1000,
		macd = 1010,
		macd_macd = 1011,
		macd_signal = 1012,
		macd_hist = 1013,

		rsi = 2000,

		ma = 3000,
		ma2 = 3001,
		ma3 = 3002,
		ema = 3010,
		ema2 = 3011,
		ema3 = 3012,
		lsma = 3020,
		lsma2 = 3021,
		lsma3 = 3022,

		bb = 3100, // bollinger band
		bb_sma = 3101,
		bb_upper = 3102,
		bb_lower = 3103
	}
}
