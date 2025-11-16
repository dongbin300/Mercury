using GeneticSharp;

using Mercury.Charts;

namespace GeneticLab.Fitnesses
{
	public class RsiTradeFitness_Account
	{
		public decimal Balance = 1_000_000;
		public decimal Btc = 0;
	}

	public class RsiTradeFitness : IFitness
	{
		public static ChartPack? Charts = null;

		/// <summary>
		/// 이 메서드를 CUDA로 작성
		/// Input: BuyRsi(double), SellRsi(double), StartTime(string, yyyy-MM-dd HH:mm:ss), EndTime(string, yyyy-MM-dd HH:mm:ss)
		/// 처리: 시작날짜부터 끝날짜까지 시그널에 의한 매수 매도를 진행하고 결과를 반환한다.
		/// Output: Balance(double)
		/// 
		/// - 추가 함수
		/// Input: Symbol(string), StartTime(string, yyyy-MM-dd HH:mm:ss), EndTime(string, yyyy-MM-dd HH:mm:ss)
		/// 처리: .csv 파일에서 차트를 불러온다.
		/// Output: 없음
		/// </summary>
		/// <param name="chromosome"></param>
		/// <returns></returns>
		public double Evaluate(IChromosome chromosome)
		{
			var values = ((FloatingPointChromosome)chromosome).ToFloatingPoints();
			var buySignalRsi = (decimal)values[0];
			var sellSignalRsi = (decimal)values[1];

			var account = new RsiTradeFitness_Account();

			if (Charts == null)
			{
				Charts = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.FiveMinutes);
				Charts.UseRsi();
			}

			var startTime = Charts.StartTime.AddMinutes(5 * RandomizationProvider.Current.GetInt(0, 100_000));
			var c1 = Charts.Select(startTime);
			var c0 = Charts.Next();

			for (int i = 0; i < 288; i++)
			{
				if (account.Btc <= 0 &&
					c1.Rsi1 < buySignalRsi &&
					c0.Rsi1 > buySignalRsi)
				{
					account.Balance -= c0.Quote.Close;
					account.Btc++;
				}
				else if (account.Btc > 0 &&
					c1.Rsi1 > sellSignalRsi &&
					c0.Rsi1 < sellSignalRsi)
				{
					account.Balance += c0.Quote.Close;
					account.Btc--;
				}

				c1 = c0;
				c0 = Charts.Next();
			}

			if (account.Btc > 0)
			{
				account.Btc = 0;
				account.Balance += c0.Quote.Close;
			}

			return (double)account.Balance;
		}
	}
}
