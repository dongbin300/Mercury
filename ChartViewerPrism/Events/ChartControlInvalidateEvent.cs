using Prism.Events;

using SkiaSharp.Views.Desktop;

namespace ChartViewerPrism.Events
{
	public class ChartControlInvalidateEvent : PubSubEvent<SKPaintSurfaceEventArgs>
	{
	}
}
