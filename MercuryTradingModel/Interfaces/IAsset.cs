using MercuryTradingModel.Assets;

namespace MercuryTradingModel.Interfaces
{
    public interface IAsset
    {
        /// <summary>
        /// 현금 잔고
        /// </summary>
        decimal Balance { get; set; }

        /// <summary>
        /// 포지션
        /// </summary>
        Position Position { get; set; }
    }
}
