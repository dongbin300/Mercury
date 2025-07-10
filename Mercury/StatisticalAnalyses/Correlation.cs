using Mercury.Charts;
using Mercury.Maths;

namespace Mercury.StatisticalAnalyses
{
	public class Correlation : BaseStatisticalAnalysis
	{
		public List<(string, double, double)> Run()
		{
			List<(string, double, double)> result = [];

			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;

				// 몸통 길이와 거래량 배열 추출
				var bodies = charts.Select(c => (double)Math.Abs(c.Quote.Open - c.Quote.Close)).ToArray();
				var volumes = charts.Select(c => (double)c.Quote.Volume).ToArray();

				// 상관계수 계산
				var pearson = StatisticalMath.PearsonCorrelation(bodies, volumes);
				var spearman = StatisticalMath.SpearmanCorrelation(bodies, volumes);

				result.Add((chartPack.Symbol, pearson, spearman));
			}

			return result;
		}
	}
}
