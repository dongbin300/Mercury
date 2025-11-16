using System;
using System.ComponentModel;

namespace Backtester2.Models
{
    public class BacktestResult : INotifyPropertyChanged
    {
        private string _strategy = string.Empty;
        private string _symbol = string.Empty;
        private string _period = string.Empty;
        private int _totalTrades;
        private int _wins;
        private int _losses;
        private decimal _winRate;
        private decimal _roi;
        private decimal _mdd;
        private decimal _score;
        private decimal _annualRoi;
        private string _parameters = string.Empty;
        private DateTime _testDate;

        public string Strategy
        {
            get => _strategy;
            set { _strategy = value; OnPropertyChanged(nameof(Strategy)); }
        }

        public string Symbol
        {
            get => _symbol;
            set { _symbol = value; OnPropertyChanged(nameof(Symbol)); }
        }

        public string Period
        {
            get => _period;
            set { _period = value; OnPropertyChanged(nameof(Period)); }
        }

        public int TotalTrades
        {
            get => _totalTrades;
            set { _totalTrades = value; OnPropertyChanged(nameof(TotalTrades)); }
        }

        public int Wins
        {
            get => _wins;
            set { _wins = value; OnPropertyChanged(nameof(Wins)); }
        }

        public int Losses
        {
            get => _losses;
            set { _losses = value; OnPropertyChanged(nameof(Losses)); }
        }

        public decimal WinRate
        {
            get => _winRate;
            set { _winRate = value; OnPropertyChanged(nameof(WinRate)); OnPropertyChanged(nameof(WinRateText)); }
        }

        public decimal Roi
        {
            get => _roi;
            set { _roi = value; OnPropertyChanged(nameof(Roi)); OnPropertyChanged(nameof(RoiText)); OnPropertyChanged(nameof(IsProfit)); }
        }

        public decimal Mdd
        {
            get => _mdd;
            set { _mdd = value; OnPropertyChanged(nameof(Mdd)); OnPropertyChanged(nameof(MddText)); }
        }

        public decimal Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(nameof(Score)); OnPropertyChanged(nameof(ScoreText)); }
        }

        public decimal AnnualRoi
        {
            get => _annualRoi;
            set { _annualRoi = value; OnPropertyChanged(nameof(AnnualRoi)); OnPropertyChanged(nameof(AnnualRoiText)); }
        }

        public string Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(nameof(Parameters)); }
        }

        public DateTime TestDate
        {
            get => _testDate;
            set { _testDate = value; OnPropertyChanged(nameof(TestDate)); }
        }

        // UI 바인딩용 속성들
        public string WinRateText => $"{WinRate:F2}%";
        public string RoiText => $"{Roi:F2}%";
        public string MddText => $"{Mdd:F2}%";
        public string ScoreText => $"{Score:F4}";
        public string AnnualRoiText => $"{AnnualRoi:F2}%";
        public bool IsProfit => Roi > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// CSV 라인에서 BacktestResult 객체 생성
        /// 예시: "TRXUSDT +4,1h,None,None,2023-01-01,2024-12-31,2025-09-17 14:04:45\nCci22,15,220,-210,0,2,3.0,1.4,0.6,1.5,3,5.5,1.5,Total,10,1,1144,896,56.08%,1030205,29.12%,3.5383"
        /// </summary>
        public static BacktestResult? FromCsvLine(string csvLine)
        {
            try
            {
                var parts = csvLine.Split(',');
                if (parts.Length < 20) return null;

                var result = new BacktestResult();

                // 기본 정보 파싱
                result.Strategy = parts[0].Trim();
                
                // 심볼 정보는 이전 라인에서 가져와야 함 (임시로 빈 값)
                result.Symbol = "N/A";
                result.Period = "N/A";

                // 거래 정보 파싱 (끝에서부터)
                var endIndex = parts.Length - 1;
                if (decimal.TryParse(parts[endIndex], out decimal score))
                    result.Score = score;

                if (decimal.TryParse(parts[endIndex - 1].Replace("%", ""), out decimal mdd))
                    result.Mdd = mdd;

                if (int.TryParse(parts[endIndex - 2], out int finalMoney))
                {
                    // 시드머니 100만원 기준으로 ROI 계산
                    result.Roi = (finalMoney - 1000000m) / 1000000m * 100;
                    
                    // 2년 기간 기준 연평균 수익률 계산 (복리)
                    if (finalMoney > 0)
                    {
                        var totalReturn = (decimal)finalMoney / 1000000m;
                        result.AnnualRoi = ((decimal)Math.Pow((double)totalReturn, 1.0 / 2.0) - 1) * 100;
                    }
                }

                if (decimal.TryParse(parts[endIndex - 3].Replace("%", ""), out decimal winRate))
                    result.WinRate = winRate;

                if (int.TryParse(parts[endIndex - 4], out int losses))
                    result.Losses = losses;

                if (int.TryParse(parts[endIndex - 5], out int wins))
                    result.Wins = wins;

                result.TotalTrades = wins + losses;

                // 파라미터 정보 (전략명 이후부터 거래정보 이전까지)
                var paramParts = new string[endIndex - 5];
                Array.Copy(parts, 1, paramParts, 0, paramParts.Length);
                result.Parameters = string.Join(",", paramParts);

                result.TestDate = DateTime.Now;

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 심볼과 기간 정보 업데이트
        /// </summary>
        public void UpdateSymbolAndPeriod(string symbol, string period)
        {
            Symbol = symbol;
            Period = period;
        }
    }
}