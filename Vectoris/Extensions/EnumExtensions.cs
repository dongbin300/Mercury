using Binance.Net.Enums;

namespace Vectoris.Extensions;

public static class EnumExtensions
{
	public static PositionSide ToPositionSide(this string side)
	{
		return Enum.Parse<PositionSide>(side, true);
	}

	//public static GridType ToGridType(this string type)
	//{
	//	return Enum.Parse<GridType>(type, true);
	//}
}
