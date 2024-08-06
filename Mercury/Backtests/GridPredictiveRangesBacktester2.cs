using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

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
		public decimal PredictiveRangesRiskMargin { get; set; }

		public GridPredictiveRangesBacktester2(string symbol, List<Price> prices, List<ChartInfo> charts, string reportFileName, int gridCount, decimal riskMargin = 0m, int leverage = 1)
		{
			Symbol = symbol;
			Prices = prices;
			Charts = charts;
			ReportFileName = reportFileName;
			GridCount = gridCount;
			GridType = GridType.Neutral;
			PredictiveRangesRiskMargin = riskMargin;
			Leverage = leverage;
		}

		public (double, decimal) Run(int startIndex)
		{
			var isLiquidation = false;
			var startTime = Prices[startIndex].Date;
			DateTime displayDate = startTime;

			SetGrid(startIndex);
			SetOrder(startIndex);

			for (int i = startIndex; i < Prices.Count; i++)
			{
				if (Prices[i].Date >= displayDate) // 결과 출력
				{
					var time = Prices[i].Date;
					var price = Prices[i].Value;

					displayDate = displayDate.AddDays(1);

					var yesterdayChart = LastChart(time); // 이전봉 종가
					var yesterday2Chart = LastChart2(time); // 2이전봉 종가

					// Long/Short 그리드인 경우 PRA 크로스하는지 주기적으로 체크
					if ((GridType == GridType.Long && yesterdayChart.Quote.Close < (decimal)yesterdayChart.PredictiveRangesAverage) ||
						(GridType == GridType.Short && yesterdayChart.Quote.Close > (decimal)yesterdayChart.PredictiveRangesAverage))
					{
						WriteStatus(i, "PRA_CROSS");
						CloseAllPositions(i);

						GridType = GridType.Neutral;

						SetGrid(i);
						SetOrder(i);
					}

					// PRA 값이 바뀜 : 포지션 정리 후 그리드 재설정
					if (yesterday2Chart.PredictiveRangesAverage != yesterdayChart.PredictiveRangesAverage)
					{
						WriteStatus(i, "CHANGE_PR");
						CloseAllPositions(i);

						GridType = yesterdayChart.Quote.Close > (decimal)yesterday2Chart.PredictiveRangesAverage ? GridType.Long : GridType.Short;

						SetGrid(i);
						SetOrder(i);
					}

					// 청산 확인
					if (GetEstimatedAsset(price) < 0)
					{
						WriteStatus(i, "LIQUIDATION");
						isLiquidation = true;
						break;
					}

					WriteStatus(i, "");
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

			var estimatedMoney = GetEstimatedAsset(Prices[^1].Value);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = isLiquidation ? -419 : ((double)(LongFillCount + ShortFillCount) / period).Round(1);

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					 $"{tradePerDay},{estimatedMoney.Round(0)}" + Environment.NewLine + Environment.NewLine);

			// Position History
			if (IsGeneratePositionHistory)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
				string.Join(Environment.NewLine, PositionHistories) + Environment.NewLine + Environment.NewLine + Environment.NewLine
				);
			}

			return (tradePerDay, estimatedMoney);
		}

		/// <summary>
		/// deprecated
		/// </summary>
		/// <param name="chartIndex"></param>
		public void ApplyLeverage(int chartIndex)
		{
			var time = Prices[chartIndex].Date;
			var price = Prices[chartIndex].Value;
			var chart = LastChart2(time);
			var leverage = (int)Calculator.RangesLeverage((decimal)chart.PredictiveRangesUpper2, (decimal)chart.PredictiveRangesLower2, price, GridCount, PredictiveRangesRiskMargin);
			Leverage = leverage;
			//Money = InitialMargin + ((Money - InitialMargin) * Leverage);
		}

		public void SetGrid(int chartIndex)
		{
			ApplyLeverage(chartIndex);
			InitialMargin = Money * 0.9m;

			var currentPrice = Prices[chartIndex].Value;
			var currentTime = Prices[chartIndex].Date;

			UpperPrice = (decimal)LastChart2(currentTime).PredictiveRangesUpper2;
			LowerPrice = (decimal)LastChart2(currentTime).PredictiveRangesLower2;
			UpperStopLossPrice = (decimal)LastChart2(currentTime).PredictiveRangesUpper2;
			LowerStopLossPrice = (decimal)LastChart2(currentTime).PredictiveRangesLower2;

			GridInterval = (UpperPrice - LowerPrice) / GridCount;
			StandardBaseOrderSize = (InitialMargin * Leverage) / GridCount;
			//var maxLeverage = LastChart(currentTime).PredictiveRangesMaxLeverage;

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{currentTime:yyyy-MM-dd HH:mm:ss},{GridType},L:{Leverage},Price:{currentPrice.Round(2)},Upper:{UpperPrice.Round(2)},Lower:{LowerPrice.Round(2)},UpperSL:{UpperStopLossPrice.Round(2)},LowerSL:{LowerStopLossPrice.Round(2)},Interval:{GridInterval.Round(2)},Count:{GridCount},BOS:{StandardBaseOrderSize.Round(2)}" + Environment.NewLine);
		}
	}
}