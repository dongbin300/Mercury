using Albedo.Models;

using System.Windows.Controls;

namespace Albedo.Views
{
    /// <summary>
    /// SymbolControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PairControl : UserControl
    {
        public Pair Pair { get; set; }

        public PairControl()
        {
            InitializeComponent();
            Pair = new Pair(Enums.PairMarket.None, Enums.PairMarketType.None, Enums.PairQuoteAsset.None, "", 0, 0);
        }

        public void Init(Pair pair)
        {
            Pair = pair;
            Tag = $"{Pair.Market}_{Pair.MarketType}_{Pair.Symbol}";
        }
    }
}
