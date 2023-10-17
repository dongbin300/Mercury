using System.Collections.Generic;

namespace CryptoProphet.Models
{
    public class Stat
    {
        //public List<StatModel> Models { get; set; }

        /// <summary>
        /// {1, 2, 3, 4, 5 -> 1->4, 3->2, 6->1},
        /// {2, 3, 4, 5, 6 -> 3->1, 4->3, 8->1}, ...
        /// </summary>
        public Dictionary<List<int>, Dictionary<int, int>> Stats { get; set; } = new(new ListEqualityComparer());
        public Dictionary<List<int>, decimal> EvaluatedStats { get; set; } = new();

        public void Add(List<int> inspection, int record)
        {
            if (Stats.ContainsKey(inspection))
            {
                if (Stats[inspection].ContainsKey(record))
                {
                    Stats[inspection][record]++;
                }
                else
                {
                    Stats[inspection].Add(record, 1);
                }
            }
            else
            {
                Stats.Add(inspection, new Dictionary<int, int>() { { record, 1 } });
            }
        }

        public void Evaluate()
        {
            EvaluatedStats.Clear();
            foreach (var stat in Stats)
            {
                var inspection = stat.Key;
                var record = stat.Value;
                var sum = 0;
                foreach (var r in record)
                {
                    sum += r.Key * r.Value;
                }
                EvaluatedStats.Add(inspection, sum);
            }
        }
    }

    public class ListEqualityComparer : IEqualityComparer<List<int>>
    {
        public bool Equals(List<int> x, List<int> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(List<int> obj)
        {
            int hashCode = 17;

            foreach (int item in obj)
            {
                hashCode = hashCode * 31 + item.GetHashCode();
            }

            return hashCode;
        }
    }
}
