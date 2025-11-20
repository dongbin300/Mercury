using System.Runtime.CompilerServices;

namespace Vectoris.Maths;

/// <summary>
/// Ultra-fast Xoshiro128++ pseudo-random generator.
/// Suitable for simulations, Monte Carlo, trading backtests.
/// </summary>
public sealed class Xoshiro128
{
	private uint _s0, _s1, _s2, _s3;

	/// <summary>
	/// Uses Environment.TickCount as default seed.
	/// </summary>
	public Xoshiro128()
		: this(Environment.TickCount)
	{
	}

	public Xoshiro128(int seed)
	{
		Initialize(seed);
	}

	/// <summary>
	/// Initialize PRNG state using SplitMix32.
	/// </summary>
	public void Initialize(int seed)
	{
		uint x = (uint)seed;
		_s0 = SplitMix32(ref x);
		_s1 = SplitMix32(ref x);
		_s2 = SplitMix32(ref x);
		_s3 = SplitMix32(ref x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint SplitMix32(ref uint x)
	{
		x += 0x9E3779B9;
		uint z = x;
		z = (z ^ (z >> 16)) * 0x85EBCA6B;
		z = (z ^ (z >> 13)) * 0xC2B2AE35;
		return z ^ (z >> 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint NextUIntCore()
	{
		// Xoshiro128++ core
		uint result = Rotl(_s1 * 5, 7) * 9;

		uint t = _s1 << 9;

		_s2 ^= _s0;
		_s3 ^= _s1;
		_s1 ^= _s2;
		_s0 ^= _s3;
		_s2 ^= t;

		_s3 = Rotl(_s3, 11);

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

	/// <summary>
	/// Returns random uint.
	/// </summary>
	public uint NextUInt() => NextUIntCore();

	/// <summary>
	/// Returns random int (0 ~ int.MaxValue).
	/// </summary>
	public int Next() => (int)(NextUIntCore() & 0x7FFFFFFF);

	/// <summary>
	/// Returns random int [0, upperBound).
	/// </summary>
	public int Next(int upperBound)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(upperBound);

		return (int)(NextDouble() * upperBound);
	}

	/// <summary>
	/// Returns random int [lowerBound, upperBound).
	/// </summary>
	public int Next(int lowerBound, int upperBound)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(lowerBound, upperBound);

		return lowerBound + Next(upperBound - lowerBound);
	}

	/// <summary>
	/// Returns random double in [0.0, 1.0).
	/// </summary>
	public double NextDouble()
	{
		ulong a = NextUIntCore();
		ulong b = NextUIntCore();
		return ((a >> 5) * 67108864.0 + (b >> 6)) / (1UL << 53);
	}

	/// <summary>
	/// Fills the buffer with random bytes.
	/// </summary>
	public void NextBytes(Span<byte> buffer)
	{
		int i = 0;
		while (i + 4 <= buffer.Length)
		{
			uint r = NextUIntCore();
			buffer[i++] = (byte)r;
			buffer[i++] = (byte)(r >> 8);
			buffer[i++] = (byte)(r >> 16);
			buffer[i++] = (byte)(r >> 24);
		}

		if (i < buffer.Length)
		{
			uint r = NextUIntCore();
			int shift = 0;
			while (i < buffer.Length)
			{
				buffer[i++] = (byte)(r >> shift);
				shift += 8;
			}
		}
	}

	/// <summary>
	/// Returns random boolean.
	/// </summary>
	public bool NextBool() => (NextUIntCore() & 1) != 0;
}
