using static Mercury.Charts.Patterns.PatternUtil;

namespace Mercury.Charts.Patterns
{
    /// <summary>
    /// - Numbering Convention
    /// [1-5][1-5][1-5][1-5]
    /// 
    /// 1st. L
    ///     1. <L 
    ///     2. <O
    ///     3. <C
    ///     4. <H
    ///     5. >H
    ///     
    /// 2nd. O
    ///     ...
    ///     
    /// 3rd. C
    ///     ...
    ///     
    /// 4th. H
    ///     ...
    /// 
    /// </summary>
    public class DeployPattern
    {
        /// <summary>
        /// Pattern number of q0
        /// </summary>
        public int Input { get; private set; }

        /// <summary>
        /// Pattern number of q1
        /// </summary>
        public int Output { get; private set; }

        //public Quote Quote0 { get; private set; }
        //public Quote Quote1 { get; private set; }


        public DeployPattern(Quote q0, Quote q1, decimal equalThreshold = 0.0005m) : this(0, q0, q1, equalThreshold)
        {
            
        }

        public DeployPattern(int input, Quote q0, Quote q1, decimal equalThreshold = 0.0005m)
        {
            Input = input;
            Output =
                GetPositionNumber(q0, q1.Low) * 1000 +
                GetPositionNumber(q0, Loc(q1)) * 100 +
                GetPositionNumber(q0, Hoc(q1)) * 10 +
                GetPositionNumber(q0, q1.High);

            //Quote0 = q0;
            //Quote1 = q1;
        }

        private int GetPositionNumber(Quote q0, decimal value)
        {
            if (value < q0.Low)
            {
                return 1;
            }
            else if (value < Loc(q0))
            {
                return 2;
            }
            else if (value < Hoc(q0))
            {
                return 3;
            }
            else if (value < q0.High)
            {
                return 4;
            }
            else
            {
                return 5;
            }
        }

        public override string ToString()
        {
            return Input + "," + Output;
        }
    }
}
