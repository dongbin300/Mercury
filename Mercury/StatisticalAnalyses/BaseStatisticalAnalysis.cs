using Mercury.Charts;

namespace Mercury.StatisticalAnalyses
{
	public class BaseStatisticalAnalysis
	{
		public List<ChartPack> ChartPacks { get; set; } = [];

		public BaseStatisticalAnalysis()
		{
			
		}

		public void Init(List<ChartPack> chartPacks)
		{
			ChartPacks = chartPacks;
		}
	}
}
