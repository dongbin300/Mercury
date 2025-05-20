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
	/// TrendRider 값에 따라 GridType 설정
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

			// 어제 TrendRider Trend에 따라 GridType 설정
			var _yesterdayChart = Charts.GetLatestChartBefore(startTime);
			startGridType = _yesterdayChart.TrendRiderTrend switch
			{
				0 => GridType.Neutral,
				1 => GridType.Long,
				-1 => GridType.Short,
				_ => startGridType
			};
			ExecuteInitGrid(startIndex, startGridType);

			for (int i = startIndex; i < Prices.Count; i++)
			{
				if (Prices[i].Date >= displayDate) // 결과 출력
				{
					var time = Prices[i].Date;
					var price = Prices[i].Value;

					displayDate = displayDate.AddDays(1);

					var yesterdayChart = Charts.GetLatestChartBefore(time); // 어제 캔들
					var yesterday2Chart = Charts.GetLatestChartBefore(time.AddDays(-1)); // 엊그제 캔들

					// TrendRider 값에 따라 GridType 설정
					if (yesterdayChart.TrendRiderTrend == 0 && Grid.GridType != GridType.Neutral)
					{
						WriteStatus(i, "CHANGE_TO_NEUTRAL");
						CloseAllPositions(i);
						ExecuteInitGrid(i, GridType.Neutral);
					}
					else if (yesterdayChart.TrendRiderTrend == 1 && Grid.GridType != GridType.Long)
					{
						WriteStatus(i, "CHANGE_TO_LONG");
						CloseAllPositions(i);
						ExecuteInitGrid(i, GridType.Long);
					}
					else if (yesterdayChart.TrendRiderTrend == -1 && Grid.GridType != GridType.Short)
					{
						WriteStatus(i, "CHANGE_TO_SHORT");
						CloseAllPositions(i);
						ExecuteInitGrid(i, GridType.Short);
					}
					// PRA 값이 바뀌어도 재설정
					else if (yesterdayChart.TrendRiderTrend == 1 && yesterdayChart.PredictiveRangesAverage != yesterday2Chart.PredictiveRangesAverage)
					{
						WriteStatus(i, "RESET_LONG");
						CloseAllPositions(i);
						ExecuteInitGrid(i, GridType.Long);
					}
					else if (yesterdayChart.TrendRiderTrend == -1 && yesterdayChart.PredictiveRangesAverage != yesterday2Chart.PredictiveRangesAverage)
					{
						WriteStatus(i, "RESET_SHORT");
						CloseAllPositions(i);
						ExecuteInitGrid(i, GridType.Short);
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

			var upperPrice = (decimal)yesterday.PredictiveRangesUpper2;
			var lowerPrice = (decimal)yesterday.PredictiveRangesLower2;
			UpperStopLossPrice = (decimal)yesterday.PredictiveRangesUpper2;
			LowerStopLossPrice = (decimal)yesterday.PredictiveRangesLower2;

			// 최근 한달간의 일봉 ATR 평균 계산
			var monthlyAtrAverage = (decimal)Charts.Where(d => d.DateTime <= currentTime).OrderByDescending(d => d.DateTime).Take(30).Average(x => x.Atr);

			var gridInterval = monthlyAtrAverage * AtrRatio;

			InitGrid(gridType, upperPrice, lowerPrice, gridInterval, chartIndex);
		}
	}
}