using System.Runtime.CompilerServices;

/// <summary>
/// Self-modifying RNG structure that implements xoroshiro128+
/// </summary>
/// <remarks>https://en.wikipedia.org/wiki/Xoroshiro128%2B</remarks>
public ref struct Xoroshiro
{
    public const ulong XOROSHIRO_CONST0= 0x0F4B17A579F18960;
    public const ulong XOROSHIRO_CONST = 0x82A2B175229D6A5B;

    private ulong s0, s1;

    public Xoroshiro(ulong s0 = XOROSHIRO_CONST0, ulong s1 = XOROSHIRO_CONST) => (this.s0, this.s1) = (s0, s1);

    public (ulong s0, ulong s1) GetState() => (s0, s1);
    public string FullState => $"{s1:X16}{s0:X16}";

    /// <summary>
    /// Gets the next random <see cref="ulong"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next()
    {
        var _s0 = s0;
        var _s1 = s1;
        var result = _s1 + _s0;

        _s1 = s1 ^ _s0;
        s0 = _s1 ^ (_s0 >> 0x28 | _s0 << 0x18) ^ _s1 << 0x10;
        s1 = _s1 >> 0x1b | _s1 << 0x25;

        return result;
    }

    /// <summary>
    /// Gets the next random <see cref="uint"/>.
    /// </summary>
    public uint NextUInt() => (uint)(Next() & 0xFFFFFFFF);

    /// <summary>
    /// Gets a random value that is less than <see cref="MOD"/>
    /// </summary>
    /// <param name="MOD">Maximum value (exclusive). Generates a bitmask for the loop.</param>
    /// <returns>Random value</returns>
    public ulong NextInt(ulong MOD = 0xFFFFFFFF)
    {
        ulong mask = GetBitmask(MOD);
        ulong res;
        do
        {
            res = Next() & mask;
        } while (res >= MOD);
        return res;
    }

    /// <summary>
    /// Next Power of Two
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetBitmask(ulong x)
    {
        x--; // comment out to always take the next biggest power of two, even if x is already a power of two
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x;
    }
}
