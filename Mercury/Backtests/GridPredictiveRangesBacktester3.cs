using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;

namespace Mercury.Backtests
{
	/// <summary>
	/// 아이디어
	/// 1. PR이 바뀐날 종가에 Neutral 종료
	/// 즉시 하루 동안 상향이면 Long, 하향이면 Short로 시작
	/// 만약 다음 날부터 종가가 PRA 크로스한 날이 오면 종가에 Long/Short 종료 후 Neutral로 전환
	/// PR이 바뀌면 바뀐 PR로 다시 Long/Short 시작
	/// 
	/// PR이 바뀌는 날에 상향돌파 시 Long Grid, 하향돌파 시 Short Grid 시작
	/// Long 혹은 Short Grid인 경우 슈퍼트렌드값이 바뀌는 순간 Neutral Grid로 전환
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="prices"></param>
	/// <param name="charts"></param>
	/// <param name="reportFileName"></param>
	/// <param name="gridCount"></param>
	public class GridPredictiveRangesBacktester3 : GridBacktester
	{
		public decimal AtrRatio = 0.2m; // ATR 비율

		public GridPredictiveRangesBacktester3(string symbol, List<Price> prices, List<ChartInfo> charts, string reportFileName) : base(symbol, prices, charts, reportFileName)
		{
		}

		public (double, decimal) Run(int startIndex, GridType startGridType = GridType.Neutral)
		{
			var isLiquidation = false;
			var startTime = Prices[startIndex].Date;
			DateTime displayDate = startTime;

			startGridType = GridType.Neutral; // 초기값 Neutral로 설정
			ExecuteInitGrid(startIndex, startGridType);

			for (int i = startIndex; i < Prices.Count; i++)
			{
				if (Prices[i].Date >= displayDate) // 결과 출력, 1일 1번
				{
					var time = Prices[i].Date;
					var price = Prices[i].Value;

					displayDate = displayDate.AddDays(1);

					var yesterdayChart = Charts.GetLatestChartBefore(time); // 어제 캔들
					var yesterday2Chart = Charts.GetLatestChartBefore(time.AddDays(-1)); // 엊그제 캔들

					// PRA가 바뀌면 그리드 재설정
					if (yesterdayChart.MercuryRangesAverage != yesterday2Chart.MercuryRangesAverage)
					{
						var gridType = yesterdayChart.Quote.Close > (decimal)yesterday2Chart.MercuryRangesAverage ? GridType.Long : GridType.Short;

						WriteStatus(i, "PRA_CHANGED");
						CloseAllPositions(i);
						ExecuteInitGrid(i, gridType);
					}

					// Long 혹은 Short Grid인 경우 슈퍼트렌드값이 바뀌는 순간 Neutral Grid로 전환
					if (Grid.GridType == GridType.Long || Grid.GridType == GridType.Short)
					{
						if (yesterdayChart.Supertrend1 * yesterday2Chart.Supertrend1 < 0) // 곱했을 때 -면 슈퍼트렌드 방향이 바뀐 것
						{
							WriteStatus(i, "SUPERTREND_CHANGED");
							CloseAllPositions(i);
							ExecuteInitGrid(i, GridType.Neutral);
						}
					}

					// 청산 확인
					if (EstimatedMoney(i) < 0)
					{
						WriteStatus(i, "LIQUIDATION");
						isLiquidation = true;
						break;
					}

					WriteStatus(i, "");
					AddDailyEstimatedMoney(i);
				}

				// 매매 Filled
				if (NearestLongOrder != null && NearestLongOrder.Price >= Prices[i].Value)
				{
					Fill(NearestLongOrder, i);
				}
				if (NearestShortOrder != null && NearestShortOrder.Price <= Prices[i].Value)
				{
					Fill(NearestShortOrder, i);
				}
			}

			var estimatedMoney = EstimatedMoney(Prices.Count - 1);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = isLiquidation ? -419 : ((double)FillCount / period).Round(1);

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					 $"{tradePerDay}/d,{estimatedMoney.Round(0)}" + Environment.NewLine + Environment.NewLine);

			// Position History
			if (IsGeneratePositionHistory)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
				string.Join(Environment.NewLine, PositionHistories) + Environment.NewLine + Environment.NewLine + Environment.NewLine
				);
			}

			return (tradePerDay, estimatedMoney);
		}

		public void ExecuteInitGrid(int chartIndex, GridType gridType)
		{
			var currentPrice = Prices[chartIndex].Value;
			var currentTime = Prices[chartIndex].Date;
			var yesterday = Charts.GetLatestChartBefore(currentTime);
			var yesterday2 = Charts.GetLatestChartBefore(currentTime.AddDays(-1));

			var upperPrice = (decimal)yesterday.MercuryRangesUpper;
			var lowerPrice = (decimal)yesterday.MercuryRangesLower;
			UpperStopLossPrice = (decimal)yesterday.MercuryRangesUpper;
			LowerStopLossPrice = (decimal)yesterday.MercuryRangesLower;

			// 최근 한달간의 일봉 ATR 평균 계산
			var monthlyAtrAverage = (decimal)Charts.Where(d => d.DateTime <= currentTime).OrderByDescending(d => d.DateTime).Take(30).Average(x => x.Atr);

			var gridInterval = monthlyAtrAverage * AtrRatio;

			InitGrid(gridType, upperPrice, lowerPrice, gridInterval, chartIndex);
		}
	}
}