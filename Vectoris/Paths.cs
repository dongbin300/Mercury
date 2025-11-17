using Vectoris.Extensions;

namespace Vectoris;

public class Paths
{
	public static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
	public static readonly string Base = @"D:\Assets";
	public static readonly string BinanceApiKey = Base.Down("binance_api.txt");
	public static readonly string BinanceFuturesData = Base.Down("BinanceFuturesData");
	public static readonly string BinanceFutures1m = BinanceFuturesData.Down("1m");
	public static readonly string BinanceFutures1D = BinanceFuturesData.Down("1D");
}
