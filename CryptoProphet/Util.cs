using CryptoProphet.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoProphet
{
    public class Util
    {
        /// <summary>
        /// 정규 분포에서 양쪽 끝 값들을 0 을 기준으로 잘라낸다.
        /// 값의 총 개수의 0.1%에 해당하는 개수를 잘라낸다.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<decimal> CutEdge(List<decimal> values)
        {
            var count = values.Count / 1000; // 0.1%
            var result = new List<decimal>(values);

            for (int i = 0; i < count; i++)
            {
                var v = result.Max(y => Math.Abs(y));
                result.Remove(result.First(x => Math.Abs(x).Equals(v)));
            }

            return result;
        }

        /// <summary>
        /// -50 ~ +50 등급으로 분류한다.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<int> Classify(List<decimal> values, int gradeCount = 50)
        {
            var result = new List<int>();
            var max = values.Max();
            var min = values.Min();

            foreach (var v in values)
            {
                var grade = (int)(gradeCount * Math.Abs(v) / (v >= 0 ? max : min));
                result.Add(grade);
            }

            return result;
        }

        /// <summary>
        /// 등급 값으로 Stat을 만든다.
        /// 이전 10봉 참조해서 +1봉을 판단한다.
        /// </summary>
        /// <param name="grades"></param>
        /// <returns></returns>
        public static Stat MakeStat(List<int> grades, int inspectionCount = 10, int recordIndex = 1)
        {
            var result = new Stat();

            for (int i = 0; i < grades.Count - inspectionCount - 1; i++)
            {
                var inspection = grades.Skip(i).Take(inspectionCount).ToList();
                var record = grades[i + inspectionCount + recordIndex];
                result.Add(inspection, record);
            }

            return result;
        }
    }
}
