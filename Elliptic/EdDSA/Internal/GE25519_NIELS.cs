﻿using System.Runtime.InteropServices;

namespace EdDSA.Internal;

/// <summary>
/// GE stands for Group Element
/// Memory-safe wrapper over fixed-length number arrays
/// TODO: Implement read-only version
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct GE25519_NIELS
{
    [FieldOffset(0 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _YsubX[ModM.ModM_WORDS];
    [FieldOffset(1 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _XaddY[ModM.ModM_WORDS];
    [FieldOffset(2 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _T2D[ModM.ModM_WORDS];

    /// <summary>
    /// All integers at once, used by constructor
    /// </summary>
    [FieldOffset(0)]
    private unsafe fixed ulong _ALL[TypeUlongSz];

    public const int TypeUlongSz = 3 * ModM.ModM_WORDS;

    public GE25519_NIELS(ReadOnlySpan<ulong> values)
    {
        // Will throw on insufficient length
        values[..TypeUlongSz].CopyTo(ALL);
    }

    public unsafe Span<ulong> YsubX
    {
        get
        {
            fixed (ulong* ptr = &_YsubX[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public unsafe Span<ulong> XaddY
    {
        get
        {
            fixed (ulong* ptr = &_XaddY[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public unsafe Span<ulong> T2D
    {
        get
        {
            fixed (ulong* ptr = &_T2D[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public unsafe Span<ulong> ALL
    {
        get
        {
            fixed (ulong* ptr = &_ALL[0])
            {
                return new(ptr, TypeUlongSz);
            }
        }
    }

}