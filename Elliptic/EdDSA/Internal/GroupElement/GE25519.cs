﻿using System.Runtime.InteropServices;

namespace EdDSA.Internal.GroupElement;

/// <summary>
/// GE stands for Group Element
/// Memory-safe wrapper over fixed-length number arrays
/// TODO: Implement read-only version
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct GE25519
{
    [FieldOffset(0 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _X[ModM.ModM_WORDS];
    [FieldOffset(1 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _Y[ModM.ModM_WORDS];
    [FieldOffset(2 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _Z[ModM.ModM_WORDS];
    [FieldOffset(3 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _T[ModM.ModM_WORDS];

    /// <summary>
    /// All integers at once, used by constructor
    /// </summary>
    [FieldOffset(0)]
    private unsafe fixed ulong _ALL[TypeUlongSz];

    public const int TypeUlongSz = 4 * ModM.ModM_WORDS;

    public GE25519(ReadOnlySpan<ulong> values)
    {
        // Will throw on insufficient length
        values[..TypeUlongSz].CopyTo(ALL);
    }

    public GE25519(in ReadOnlyGE25519 ge)
    {
        ge.ALL.CopyTo(ALL);
    }

    public readonly unsafe Span<ulong> X
    {
        get {
            fixed (ulong* ptr = &_X[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe Span<ulong> Y
    {
        get
        {
            fixed (ulong* ptr = &_Y[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe Span<ulong> Z
    {
        get
        {
            fixed(ulong* ptr = &_Z[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe Span<ulong> T
    {
        get
        {
            fixed (ulong* ptr = &_T[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe Span<ulong> ALL
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

/// <summary>
/// GE stands for Group Element
/// Memory-safe wrapper over fixed-length number arrays
/// TODO: Implement read-only version
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct ReadOnlyGE25519
{
    [FieldOffset(0 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _X[ModM.ModM_WORDS];
    [FieldOffset(1 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _Y[ModM.ModM_WORDS];
    [FieldOffset(2 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _Z[ModM.ModM_WORDS];
    [FieldOffset(3 * ModM.ModM_WORDS * sizeof(ulong))]
    private unsafe fixed ulong _T[ModM.ModM_WORDS];

    /// <summary>
    /// All integers at once, used by constructor
    /// </summary>
    [FieldOffset(0)]
    private unsafe fixed ulong _ALL[TypeUlongSz];

    public const int TypeUlongSz = 4 * ModM.ModM_WORDS;

    public ReadOnlyGE25519(ReadOnlySpan<ulong> values)
    {
        // Will throw on insufficient length
        values[..TypeUlongSz].CopyTo(_ALL_);
    }

    public ReadOnlyGE25519(in GE25519 ge)
    {
        ge.ALL.CopyTo(_ALL_);
    }

    public readonly unsafe ReadOnlySpan<ulong> X
    {
        get
        {
            fixed (ulong* ptr = &_X[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe ReadOnlySpan<ulong> Y
    {
        get
        {
            fixed (ulong* ptr = &_Y[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe ReadOnlySpan<ulong> Z
    {
        get
        {
            fixed (ulong* ptr = &_Z[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe ReadOnlySpan<ulong> T
    {
        get
        {
            fixed (ulong* ptr = &_T[0])
            {
                return new(ptr, ModM.ModM_WORDS);
            }
        }
    }

    public readonly unsafe ReadOnlySpan<ulong> ALL => _ALL_;

    private readonly unsafe Span<ulong> _ALL_
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
