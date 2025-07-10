using System.Security.Cryptography;

namespace Mercury.Maths
{
	public class SmartRandom
	{
		public long Seed { get; set; }
		private int _GetRandomInt() => RandomNumberGenerator.GetInt32(int.MaxValue);

		public SmartRandom()
		{
			Seed = _GetRandomInt() % 1337;
		}

		public SmartRandom(long seed)
		{
			Seed = seed;
		}

		/// <summary>
		/// Range: 0 ~ 2,147,483,647(int.MaxValue)
		/// </summary>
		/// <returns></returns>
		public int Next()
		{
			Seed = (Seed + _GetRandomInt()) % long.MaxValue;
			return (int)((_GetRandomInt() * Seed % int.MaxValue + int.MaxValue) % int.MaxValue);
		}

		/// <summary>
		/// Range: min ~ max-1
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public int Next(int min, int max)
		{
			return Next() % (max - min) + min;
		}

		/// <summary>
		/// Range: 0 ~ max-1
		/// </summary>
		/// <param name="max"></param>
		/// <returns></returns>
		public int Next(int max)
		{
			return Next() % max;
		}

		public string Next(string str)
		{
			return str[Next(str.Length)].ToString();
		}

		public string Next(string str, int count)
		{
			string result = string.Empty;
			for (int i = 0; i < count; i++)
			{
				result += Next(str);
			}
			return result;
		}

		public T Next<T>(IEnumerable<T> values)
		{
			return values.ElementAt(Next(values.Count()));
		}

		public DateTime Next(DateTime startTime, DateTime endTime)
		{
			return startTime.Date.AddDays(Next(0, (endTime.Date - startTime.Date).Days + 1));
		}

		/// <summary>
		/// Range: 0.0 ~ 1.0
		/// </summary>
		/// <returns></returns>
		public double NextDouble()
		{
			return (double)Next() / int.MaxValue;
		}

		public double NextDouble(double min, double max)
		{
			return NextDouble() % (max - min) + min;
		}

		public double NextDouble(double max)
		{
			return NextDouble() % max;
		}

		/// <summary>
		/// Range: 0.0 ~ 1.0
		/// </summary>
		/// <returns></returns>
		public decimal NextDecimal()
		{
			return (decimal)Next() / int.MaxValue;
		}

		public decimal NextDecimal(decimal min, decimal max)
		{
			return NextDecimal() % (max - min) + min;
		}

		public decimal NextDecimal(decimal max)
		{
			return NextDecimal() % max;
		}

		/// <summary>
		/// Normal Distribution Random
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public int NextNd(int min, int max)
		{
			double u1 = 1.0 - NextDouble();
			double u2 = 1.0 - NextDouble();
			double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

			double mean = (min + max) / 2.0;
			double stddev = (max - min) / 6.0;

			double randNormal = mean + stddev * randStdNormal;

			return (int)Math.Round(Math.Clamp(randNormal, min, max));
		}

		/// <summary>
		/// Normal Distribution Random Double
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public double NextNd(double min, double max)
		{
			double u1 = 1.0 - NextDouble();
			double u2 = 1.0 - NextDouble();
			double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

			double mean = (min + max) / 2.0;
			double stddev = (max - min) / 6.0;

			double randNormal = mean + stddev * randStdNormal;

			return Math.Clamp(randNormal, min, max);
		}

		/// <summary>
		/// Normal Distribution Random Decimal (Slower twice than Double)
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public decimal NextNd(decimal min, decimal max)
		{
			double u1 = 1.0 - NextDouble();
			double u2 = 1.0 - NextDouble();
			double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

			decimal mean = (min + max) / 2;
			decimal stddev = (max - min) / 6;

			decimal randNormal = mean + stddev * (decimal)randStdNormal;

			return Math.Clamp(randNormal, min, max);
		}
	}
}
