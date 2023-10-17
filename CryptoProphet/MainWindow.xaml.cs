using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Charts.Patterns;
using Mercury.Maths;

using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CryptoProphet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] symbols =
            {
            "ADAUSDT",
            "DOGEUSDT",
            "ETHUSDT",
            "TRXUSDT",
            "BTCUSDT",
            "XRPUSDT",
            "MTLUSDT",
            "TUSDT",
            "JASMYUSDT"
        };
        KlineInterval klineInterval = KlineInterval.FiveMinutes;

        public MainWindow()
        {
            InitializeComponent();

            var patterns = new List<DeployPattern>();
            foreach (var symbol in symbols)
            {
                ChartLoader.InitChartsMByDate(symbol, klineInterval);
                var pack = ChartLoader.GetChartPack(symbol, klineInterval);

                int temp = 0;
                for (int i = 0; i < pack.Charts.Count - 1; i++)
                {
                    var chart0 = pack.Charts[i];
                    var chart1 = pack.Charts[i + 1];
                    var pattern = new DeployPattern(temp, chart0.Quote, chart1.Quote);
                    patterns.Add(pattern);

                    temp = pattern.Output;
                }
            }


            var result = patterns.GroupBy(x => x.Input)
                .Select(x => new
                {
                    Input = x.Key,
                    Output = x.GroupBy(x => x.Output)
                    .Select(y => new
                    {
                        Output = y.Key,
                        Count = y.Count()
                    }).OrderBy(x=>x.Output).ToList()
                }).OrderBy(x=>x.Input).ToList();

            //patterns = Util.CutEdge(patterns);
            //var grades = Util.Classify(patterns, 25);
            //var stat = Util.MakeStat(grades, 5, 1);
            //stat.Evaluate();
        }
    }
}
