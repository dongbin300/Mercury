using System;

namespace Backtester
{
	public class Common
	{
		public static Action<int> ReportProgress = default!;
		public static Action<int, int> ReportProgressCount = default!;
	}
}
