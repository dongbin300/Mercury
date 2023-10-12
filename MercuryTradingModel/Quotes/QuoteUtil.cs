using Mercury;

namespace MercuryTradingModel.Quotes
{
    public class QuoteUtil
    {
        public static List<Quote> GetQuotesFromLocal(string symbol, DateTime date)
        {
            try
            {
                var result = new List<Quote>();
                var data = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Down("Gaten", "BinanceFuturesData", "1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv"));
                foreach (var d in data)
                {
                    var e = d.Split(',');
                    result.Add(new Quote
                    {
                        Date = DateTime.Parse(e[0]),
                        Open = decimal.Parse(e[1]),
                        High = decimal.Parse(e[2]),
                        Low = decimal.Parse(e[3]),
                        Close = decimal.Parse(e[4]),
                        Volume = decimal.Parse(e[5])
                    });
                }

                return result;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
        }
    }
}
