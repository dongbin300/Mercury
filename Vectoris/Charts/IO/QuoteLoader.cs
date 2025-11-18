using Binance.Net.Enums;

using System.Globalization;

using Vectoris.Charts.Core;
using Vectoris.Extensions;

namespace Vectoris.Charts.IO
{
	/// <summary>
	/// Quote 데이터를 다양한 소스에서 로드하는 헬퍼
	/// </summary>
	public static class QuoteLoader
	{
		/// <summary>
		/// CSV 파일에서 Quote 데이터 로드
		/// </summary>
		/// <param name="symbol">심볼</param>
		/// <param name="interval">시간 간격</param>
		/// <param name="startDate">시작일 (선택적)</param>
		/// <param name="endDate">종료일 (선택적)</param>
		/// <returns>Quote 데이터 리스트</returns>
		public static List<Quote> FromCsv(string symbol, KlineInterval interval, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var quotes = new List<Quote>();

				var searchDirectory = Paths.BinanceFuturesData.Down(interval switch
				{
					KlineInterval.OneMinute => $"1m\\{symbol}",
					KlineInterval.ThreeMinutes => $"1m\\{symbol}",
					KlineInterval.FiveMinutes => "5m",
					KlineInterval.FifteenMinutes => "5m",
					KlineInterval.ThirtyMinutes => "5m",
					KlineInterval.OneHour => "1h",
					KlineInterval.TwoHour => "1h",
					KlineInterval.FourHour => "1h",
					KlineInterval.SixHour => "1h",
					KlineInterval.EightHour => "1h",
					KlineInterval.TwelveHour => "1h",
					KlineInterval.OneDay => "1D",
					KlineInterval.OneWeek => "1D",
					KlineInterval.OneMonth => "1D",
					_ => $"1m\\{symbol}"
				});

				if (!Directory.Exists(searchDirectory))
					throw new DirectoryNotFoundException($"데이터 디렉토리를 찾을 수 없습니다: {searchDirectory}");

				var files = Directory.GetFiles(searchDirectory, "*.csv")
					.Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(symbol, StringComparison.OrdinalIgnoreCase))
					.OrderBy(f => f)
					.ToList();

				if (!files.Any())
					throw new FileNotFoundException($"심볼 '{symbol}'에 대한 CSV 파일을 찾을 수 없습니다: {searchDirectory}");

				foreach (var file in files)
				{
					// 날짜 범위 필터링이 있는 경우 파일명 확인
					if (startDate != null || endDate != null)
					{
						var fileName = Path.GetFileNameWithoutExtension(file);
						var fileNameParts = fileName.Split('_');

						if (fileNameParts.Length >= 2 && DateTime.TryParse(fileNameParts[1], out DateTime fileDate))
						{
							if ((startDate != null && fileDate < startDate) || (endDate != null && fileDate > endDate))
								continue;
						}
					}

					var lines = File.ReadAllLines(file);

					foreach (var line in lines)
					{
						if (string.IsNullOrWhiteSpace(line))
							continue;

						var e = line.Split(',');

						if (e.Length < 5)
							continue;

						if (!DateTime.TryParse(e[0], out DateTime date))
							continue;

						if ((startDate != null && date < startDate) || (endDate != null && date > endDate))
							continue;

						decimal open = decimal.Parse(e[1], CultureInfo.InvariantCulture);
						decimal high = decimal.Parse(e[2], CultureInfo.InvariantCulture);
						decimal low = decimal.Parse(e[3], CultureInfo.InvariantCulture);
						decimal close = decimal.Parse(e[4], CultureInfo.InvariantCulture);
						decimal volume = e.Length >= 6 ? decimal.Parse(e[5], CultureInfo.InvariantCulture) : 0;

						var quote = new Quote(date, open, high, low, close, volume);
						quotes.Add(quote);
					}
				}

				// 시간순 정렬 (오름차순)
				quotes.Sort((a, b) => a.Time.CompareTo(b.Time));

				// Convert Interval - 기본 간격이 아닌 경우 더 높은 간격으로 변환
				if (!(interval == KlineInterval.OneMinute ||
					interval == KlineInterval.FiveMinutes ||
					interval == KlineInterval.OneHour ||
					interval == KlineInterval.OneDay))
				{
					quotes = quotes.Aggregate(interval);
				}

				return quotes;
			}
			catch
			{
				throw;
			}
		}
	}
}