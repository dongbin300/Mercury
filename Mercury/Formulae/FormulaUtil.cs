using Mercury.Enums;

namespace Mercury.Formulae
{
	public class FormulaUtil
	{
		public static MtmComparison ToComparison(string data) => data switch
		{
			"<" => MtmComparison.LessThan,
			"<=" => MtmComparison.LessThanOrEqual,
			">" => MtmComparison.GreaterThan,
			">=" => MtmComparison.GreaterThanOrEqual,
			"=" => MtmComparison.Equal,
			"!=" => MtmComparison.NotEqual,
			_ => MtmComparison.None
		};

		public static string ComparisonToString(MtmComparison comparison) => comparison switch
		{
			MtmComparison.LessThan => "<",
			MtmComparison.LessThanOrEqual => "<=",
			MtmComparison.GreaterThan => ">",
			MtmComparison.GreaterThanOrEqual => ">=",
			MtmComparison.Equal => "=",
			MtmComparison.NotEqual => "!=",
			_ => "",
		};

		public static MtmCross ToCross(string data) => data switch
		{
			"++" => MtmCross.GoldenCross,
			"--" => MtmCross.DeadCross,
			_ => MtmCross.None
		};

		public static string CrossToString(MtmCross cross) => cross switch
		{
			MtmCross.GoldenCross => "++",
			MtmCross.DeadCross => "--",
			_ => ""
		};
	}
}
