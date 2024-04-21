using System.Runtime.InteropServices;
using System.Windows;

namespace ChartViewerPrism.Apis
{
	public class WinApi
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}

		[DllImport("User32.dll")]
		public static extern bool GetCursorPos(ref POINT lpPoint);

		public static Point GetCursorPosition()
		{
			var lpPoint = new POINT();
			GetCursorPos(ref lpPoint);
			return new Point(lpPoint.X, lpPoint.Y);
		}
	}
}
