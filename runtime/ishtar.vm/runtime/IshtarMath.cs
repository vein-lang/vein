// ReSharper disable MethodNameNotMeaningful
#pragma warning disable CS8981
#pragma warning disable IDE1006
#pragma warning disable IDE1006
namespace ishtar;

public static class IshtarMath
{
    #region min

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int min(int x, int y) => x < y ? x : y;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long min(long x, long y) => x < y ? x : y;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint min(uint x, uint y) => x < y ? x : y;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong min(ulong x, ulong y) => x < y ? x : y;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float min(float x, float y) => float.IsNaN(y) || x < y ? x : y;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double min(double x, double y) => double.IsNaN(y) || x < y ? x : y;

    #endregion


    #region lzt

    [StructLayout(LayoutKind.Explicit)]
    internal struct LongDoubleUnion
    {
        [FieldOffset(0)]
        public long longValue;
        [FieldOffset(0)]
        public double doubleValue;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int lzcnt(int x) { return lzcnt((uint)x); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int lzcnt(long x) { return lzcnt((ulong)x); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int lzcnt(uint x)
    {
        if (x == 0)
            return 32;
        LongDoubleUnion u;
        u.doubleValue = 0.0;
        u.longValue = 0x4330000000000000L + x;
        u.doubleValue -= 4503599627370496.0;
        return 0x41E - (int)(u.longValue >> 52);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int lzcnt(ulong x)
    {
        if (x == 0)
            return 64;

        uint xh = (uint)(x >> 32);
        uint bits = xh != 0 ? xh : (uint)x;
        int offset = xh != 0 ? 0x41E : 0x43E;

        LongDoubleUnion u;
        u.doubleValue = 0.0;
        u.longValue = 0x4330000000000000L + bits;
        u.doubleValue -= 4503599627370496.0;
        return offset - (int)(u.longValue >> 52);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int tzcnt(int x) { return tzcnt((uint)x); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int tzcnt(long x) { return tzcnt((ulong)x); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int tzcnt(uint x)
    {
        if (x == 0)
            return 32;

        x &= (uint)-x;
        LongDoubleUnion u;
        u.doubleValue = 0.0;
        u.longValue = 0x4330000000000000L + x;
        u.doubleValue -= 4503599627370496.0;
        return (int)(u.longValue >> 52) - 0x3FF;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int tzcnt(ulong x)
    {
        if (x == 0)
            return 64;

        x = x & (ulong)-(long)x;
        uint xl = (uint)x;

        uint bits = xl != 0 ? xl : (uint)(x >> 32);
        int offset = xl != 0 ? 0x3FF : 0x3DF;

        LongDoubleUnion u;
        u.doubleValue = 0.0;
        u.longValue = 0x4330000000000000L + bits;
        u.doubleValue -= 4503599627370496.0;
        return (int)(u.longValue >> 52) - offset;
    }
    #endregion

    #region max

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int max(int x, int y) => x > y ? x : y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint max(uint x, uint y) => x > y ? x : y;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long max(long x, long y) => x > y ? x : y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong max(ulong x, ulong y) => x > y ? x : y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float max(float x, float y) => float.IsNaN(y) || x > y ? x : y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double max(double x, double y) => double.IsNaN(y) || x > y ? x : y;

    #endregion

    #region lerp

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float lerp(float start, float end, float t) => start + t * (end - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double lerp(double start, double end, double t) => start + t * (end - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float unlerp(float start, float end, float x) => (x - start) / (end - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double unlerp(double start, double end, double x) => (x - start) / (end - start);

    #endregion



    #region map

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float remap(float srcStart, float srcEnd, float dstStart, float dstEnd, float x) => lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double remap(double srcStart, double srcEnd, double dstStart, double dstEnd, double x) => lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x));

    #endregion


    #region clamp

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int clamp(int valueToClamp, int lowerBound, int upperBound) => max(lowerBound, min(upperBound, valueToClamp));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint clamp(uint valueToClamp, uint lowerBound, uint upperBound) => max(lowerBound, min(upperBound, valueToClamp));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long clamp(long valueToClamp, long lowerBound, long upperBound) => max(lowerBound, min(upperBound, valueToClamp));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong clamp(ulong valueToClamp, ulong lowerBound, ulong upperBound) => max(lowerBound, min(upperBound, valueToClamp));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float clamp(float valueToClamp, float lowerBound, float upperBound) => max(lowerBound, min(upperBound, valueToClamp));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double clamp(double valueToClamp, double lowerBound, double upperBound) => max(lowerBound, min(upperBound, valueToClamp));

    #endregion


    #region abs

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int abs(int x) => max(-x, x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long abs(long x) => max(-x, x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float abs(float x) => asfloat(asuint(x) & 0x7FFFFFFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double abs(double x) => asdouble(asulong(x) & 0x7FFFFFFFFFFFFFFF);

    #endregion

    #region ceil, round, etc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ceil(float x) => MathF.Ceiling(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ceil(double x) => Math.Ceiling(x);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float round(float x) => MathF.Round(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double round(double x) => Math.Round(x);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ceil_pow2(int x)
    {
        x -= 1;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ceil_pow2(uint x)
    {
        x -= 1;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ceil_pow2(long x)
    {
        x -= 1;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x |= x >> 32;
        return x + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ceil_pow2(ulong x)
    {
        x -= 1;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x |= x >> 32;
        return x + 1;
    }

    #endregion

    #region pow & sqrt

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float pow(float x, float y) => MathF.Pow(x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double pow(double x, double y) => Math.Pow(x, y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float sqrt(float x) => MathF.Sqrt(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double sqrt(double x) => Math.Sqrt(x);

    #endregion


    #region converters

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float asfloat(int x) => *(float*)&x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe float asfloat(uint x) => *(float*)&x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe double asdouble(long x) => *(double*)&x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe double asdouble(ulong x) => *(double*)&x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint asuint(float x) => *(uint*)&x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint asuint(int x) => (uint)x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong asulong(long x) => (ulong)x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong asulong(double x) => *(ulong*)&x;

    #endregion
}
