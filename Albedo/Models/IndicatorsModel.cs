using System.Collections.Generic;

namespace Albedo.Models
{
    public class IndicatorsModel
    {
        public List<MaModel> Mas { get; set; } = new ();
        public List<BbModel> Bbs { get; set; } = new ();
        public IcModel Ic { get; set; } = default!;
        public RsiModel Rsi { get; set; } = default!;
    }
}
