namespace Mercury.Indicators
{
    public record IndicatorData(DateTime Date, decimal? Value);

    public record AdxResult(DateTime Date, decimal? Adx);
	public record AtrmaResult(DateTime Date, decimal? Atrma);
	public record AtrResult(DateTime Date, decimal? Atr);
    public record BbResult(DateTime Date, decimal? Sma, decimal? Upper, decimal? Lower);
    public record CandleScoreResult(DateTime Date, decimal? CandleScore);
    public record CciResult(DateTime Date, decimal? Cci);
	public record CustomResult(DateTime Date, decimal? Upper, decimal? Lower, decimal? Pioneer, decimal? Player);
	public record DonchianChannelResult(DateTime Date, decimal? Basis, decimal? Upper, decimal? Lower);
	public record ElderRayPowerResult(DateTime Date, decimal? BullPower, decimal? BearPower);
    public record EmaResult(DateTime Date, decimal? Ema);
	/// <summary>
	/// Exponential Volume Weighted Average Price (EVWAP)
	/// </summary>
	/// <param name="Date"></param>
	/// <param name="Evwap"></param>
	public record EvwapResult(DateTime Date, decimal? Evwap);
    public record EwmacResult(DateTime Date, decimal? Ewmac);
    public record IchimokuCloudResult(DateTime Date, decimal? Conversion, decimal? Base, decimal? TrailingSpan, decimal? LeadingSpan1, decimal? LeadingSpan2);
    public record JmaSlopeResult(DateTime Date, decimal? JmaSlope);
	/// <summary>
	/// Least Square Moving Average (LSMA)
	/// </summary>
	/// <param name="Date"></param>
	/// <param name="Lsma"></param>
	public record LsmaResult(DateTime Date, decimal? Lsma);
    public record MacdResult(DateTime Date, decimal? Macd, decimal? Signal, decimal? Hist);
	public record MercuryRangesResult(DateTime Date, decimal? Upper, decimal? Average, decimal? Lower);
	/// <summary>
	/// Machine Learning Momentum Index with Pivot (MLMIP)
	/// </summary>
	/// <param name="Date"></param>
	/// <param name="Prediction"></param>
	/// <param name="PredictionMa"></param>
	public record MlmipResult(DateTime Date, decimal? Prediction, decimal? PredictionMa);
	public record PredictiveRangesResult(DateTime Date, decimal? Upper2, decimal? Upper, decimal? Average, decimal? Lower, decimal? Lower2);
	/// <summary>
	/// Rubber Index by Gaten
	/// </summary>
	/// <param name="date"></param>
	/// <param name="Ri"></param>
	public record RiResult(DateTime Date, decimal? Ri);
	/// <summary>
	/// Rolling Volume Weighted Average Price (Rolling-VWAP)
	/// </summary>
	/// <param name="Date"></param>
	/// <param name="RollingVwap"></param>
	public record RollingVwapResult(DateTime Date, decimal? RollingVwap);
	public record RsiResult(DateTime Date, decimal? Rsi);
	public record RsimaResult(DateTime Date, decimal? Rsima);
	public record SmaResult(DateTime Date, decimal? Sma);
	public record SqueezeMomentumResult(DateTime Date, decimal? Value, int? Direction, int? Signal);
    public record StdevResult(DateTime Date, decimal? Stdev);
    public record StochasticRsiResult(DateTime Date, decimal? K, decimal? D);
	public record StochResult(DateTime Date, decimal? Stoch);
	public record SupertrendResult(DateTime Date, decimal? Supertrend);
	public record TrendRiderResult(DateTime Date, decimal? Trend, decimal? Supertrend);
    public record TripleSupertrendResult(DateTime Date, decimal? Supertrend1, decimal? Supertrend2, decimal? Supertrend3);
	/// <summary>
	/// Time Segmented Volume (TSV)
	/// </summary>
	/// <param name="Date"></param>
	/// <param name="Tsv"></param>
	public record TsvResult(DateTime Date, decimal? Tsv);
    public record VolatilityRatioResult(DateTime Date, decimal? VolatilityRatio);
	/// <summary>
	/// Volume Weighted Average Price (VWAP)
	/// </summary>
	/// <param name="Date"></param>
	/// <param name="Vwap"></param>
	public record VwapResult(DateTime Date, decimal? Vwap);
    public record WmaResult(DateTime Date, decimal? Wma);
}
