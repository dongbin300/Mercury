using Mercury.Charts;

using Prism.Events;

using System.Collections.Generic;

namespace ChartViewerPrism.Events
{
	public class ChartControlSetChartsEvent : PubSubEvent<IEnumerable<ChartInfo>>
	{
	}
}
