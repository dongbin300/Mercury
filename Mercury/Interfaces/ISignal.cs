using Mercury.Assets;
using Mercury.Charts;

namespace Mercury.Interfaces
{
	public interface ISignal
	{
		IFormula Formula { get; set; }
		abstract bool IsFlare(Asset asset, ChartInfo chart, ChartInfo prevChart);
	}
}
