using System.Collections.Generic;

namespace CryptoProphet.Models
{
    public class StatModel
    {
        public List<int> Inputs { get; set; }
        public List<Dictionary<int, int>> Outputs { get; set; }

        public StatModel()
        {

        }
    }
}
