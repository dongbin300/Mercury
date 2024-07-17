using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests
{
	/// <summary>
	/// 아이디어
	/// 1. PR이 바뀐날 종가에 Neutral 종료
	/// 즉시 하루 동안 상향이면 Long, 하향이면 Short로 시작
	/// 만약 다음 날부터 종가가 PRA 크로스한 날이 오면 종가에 Long/Short 종료 후 Neutral로 전환
	/// PR이 바뀌면 바뀐 PR로 다시 Long/Short 시작
	/// 
	/// 바이낸스 백테스트 기능 써보기
	/// 하나의 파라미터로 백테스터가 정상적으로 작동하는지 확인 후 파라미터 찾기
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="prices"></param>
	/// <param name="charts"></param>
	/// <param name="reportFileName"></param>
	/// <param name="gridCount"></param>
	public class GridPredictiveRangesBacktester2 : GridBacktester
	{
        public GridPredictiveRangesBacktester2(string symbol, List<Price> prices, List<ChartInfo> charts, string reportFileName, int gridCount)
        {
			Symbol = symbol;
			Prices = prices;
			Charts = charts;
			ReportFileName = reportFileName;
			GridCount = gridCount;
        }

        public (double, int) Run(int startIndex)
		{
			var startTime = Prices[startIndex].Date;
			DateTime displayDate = startTime;

			SetGrid(startIndex);
			SetOrder(startIndex);

			for (int i = startIndex; i < Prices.Count; i++)
			{
				var time = Prices[i].Date;
				var price = Prices[i].Value;

				if (time >= displayDate)
				{
					// 결과 출력
					displayDate = displayDate.AddDays(1);
					WriteStatus(i);

					var todayChart = CurrentChart(time); // 현재봉 시가
					var yesterdayChart = LastChart(time); // 이전봉 종가
					var yesterday2Chart = LastChart2(time); // 2이전봉 종가

					// Long/Short 그리드인 경우 PRA 크로스하는지 주기적으로 체크
					if ((GridType == GridType.Long && yesterdayChart.Quote.Close < (decimal)yesterdayChart.PredictiveRangesAverage) ||
						(GridType == GridType.Short && yesterdayChart.Quote.Close > (decimal)yesterdayChart.PredictiveRangesAverage))
					{
						WriteStatus(i);
						CloseAllPositions(i);
						ApplyLeverage(i);

						GridType = GridType.Neutral;

						SetGrid(i);
						SetOrder(i);
						WriteStatus(i);
					}

					// PRA 값이 바뀜 : 포지션 정리 후 그리드 재설정
					if (yesterday2Chart.PredictiveRangesAverage != todayChart.PredictiveRangesAverage)
					{
						WriteStatus(i);
						CloseAllPositions(i);
						ApplyLeverage(i);

						GridType = todayChart.CandlestickType == CandlestickType.Bullish ? GridType.Long : GridType.Short;

						SetGrid(i);
						SetOrder(i);
						WriteStatus(i);
					}
				}

				// 매매 Filled
				if (NearestLongOrder != null && NearestLongOrder.Price >= price)
				{
					Fill(NearestLongOrder, i);
				}
				if (NearestShortOrder != null && NearestShortOrder.Price <= price)
				{
					Fill(NearestShortOrder, i);
				}
			}

			var estimatedMoney = (int)GetEstimatedAsset(Prices[^1].Value);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = ((double)(LongFillCount + ShortFillCount) / period).Round(1);

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					 $"{tradePerDay},{estimatedMoney}" + Environment.NewLine + Environment.NewLine);

			// Position History
			//File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
			//	string.Join(Environment.NewLine, PositionHistories) + Environment.NewLine + Environment.NewLine + Environment.NewLine
			//	);

			return (tradePerDay, estimatedMoney);
		}

		public void SetGrid(int chartIndex)
		{
			InitialMargin = Money;

			var currentPrice = Prices[chartIndex].Value;
			var currentTime = Prices[chartIndex].Date;

			UpperPrice = (decimal)CurrentChart(currentTime).PredictiveRangesUpper2;
			LowerPrice = (decimal)CurrentChart(currentTime).PredictiveRangesLower2;
			UpperStopLossPrice = (decimal)CurrentChart(currentTime).PredictiveRangesUpper2;
			LowerStopLossPrice = (decimal)CurrentChart(currentTime).PredictiveRangesLower2;

			GridInterval = (UpperPrice - LowerPrice) / GridCount;
			StandardBaseOrderSize = InitialMargin / GridCount;

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{currentTime:yyyy-MM-dd HH:mm:ss},{GridType},CurrentPrice:{currentPrice.Round(2)},Upper:{UpperPrice.Round(2)},Lower:{LowerPrice.Round(2)},UpperStopLoss:{UpperStopLossPrice.Round(2)},LowerStopLoss:{LowerStopLossPrice.Round(2)},GridInterval:{GridInterval.Round(2)},GridCount:{GridCount},StandardBaseOrderSize:{StandardBaseOrderSize.Round(2)}" + Environment.NewLine);
		}
	}
}