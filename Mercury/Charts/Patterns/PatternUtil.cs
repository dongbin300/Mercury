namespace Mercury.Charts.Patterns
{
    public class PatternUtil
    {
        public static decimal EqualThreshold = 0.005m;

        public static QuoteRelation Relation(decimal value1, decimal value2)
        {
            if (Greater(value1, value2))
            {
                return QuoteRelation.Greater;
            }
            else if (Less(value1, value2))
            {
                return QuoteRelation.Less;
            }
            else
            {
                return QuoteRelation.Equal;
            }
        }

        public static bool Greater(decimal value1, decimal value2)
        {
            return value1 > value2 + (value1 + value2) / 2 * EqualThreshold;
        }

        public static bool Less(decimal value1, decimal value2)
        {
            return value1 < value2 + (value1 + value2) / 2 * EqualThreshold;
        }

        public static bool Equal(decimal value1, decimal value2)
        {
            return !Greater(value1, value2) && !Less(value1, value2);
        }

        public static decimal Loc(Quote q) => q.Open < q.Close ? q.Open : q.Close;

        public static decimal Hoc(Quote q) => q.Open < q.Close ? q.Close : q.Open;
    }
}
