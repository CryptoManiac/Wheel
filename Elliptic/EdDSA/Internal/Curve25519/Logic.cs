﻿using System.Runtime.CompilerServices;

namespace Wheel.Crypto.Elliptic.EdDSA.Internal;

internal static partial class Curve25519
{

    /// <summary>
    /// Timing safe memory compare
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y, int len)
    {
        int diff = 0;
        for (int i = 0; i != len; ++i)
        {
            diff |= x[i] ^ y[i];
        }
        return !Convert.ToBoolean(diff);
    }

    /// <summary>
    /// Xor every byte of X with every byte of Y
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="len"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Xor(Span<byte> x, ReadOnlySpan<byte> y, int len)
    {
        for (int i = 0; i != len; ++i)
        {
             x[i] ^= y[i];
        }
    }
}
