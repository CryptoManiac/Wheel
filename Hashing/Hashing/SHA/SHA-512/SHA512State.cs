﻿using System.Net;
using System.Runtime.InteropServices;
namespace Wheel.Hashing.SHA.SHA512.Internal;

/// <summary>
/// Represents the state data for the 512-bit family of SHA functions
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct InternalSHA512State
{
    /// <summary>
    /// Instantiate from array or a variable number of arguments
    /// </summary>
    /// <param name="ulongs"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public InternalSHA512State(params ulong[] ulongs)
    {
        if (ulongs.Length != TypeUlongSz)
        {
            throw new ArgumentOutOfRangeException(nameof(ulongs), ulongs.Length, "Must provide " + TypeUlongSz + " arguments exactly");
        }
        
        a = ulongs[0];
        b = ulongs[1];
        c = ulongs[2];
        d = ulongs[3];
        e = ulongs[4];
        f = ulongs[5];
        g = ulongs[6];
        h = ulongs[7];
    }

    public void Add(in InternalSHA512State state)
    {
        a += state.a;
        b += state.b;
        c += state.c;
        d += state.d;
        e += state.e;
        f += state.f;
        g += state.g;
        h += state.h;
    }

    /// <summary>
    /// Dump vector contents
    /// </summary>
    /// <param name="bytes"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public readonly void Store(Span<byte> to)
    {
        int byteSz = TypeByteSz;

        if (to.Length > byteSz)
        {
            throw new ArgumentOutOfRangeException(nameof(to), to.Length, "Span must not be longer than " + byteSz + " bytes");
        }

        switch (to.Length)
        {
            case 28:
            case 32:
            case 48:
            case 64:
                {
                    // Cast to a set of 64-bit integers
                    Span<ulong> X = MemoryMarshal.Cast<byte, ulong>(to);

                    // 0 .. 2 for SHA512_224, SHA512_256, SHA-384 and SHA-512
                    X[0] = a;
                    X[1] = b;
                    X[2] = c;

                    if (X.Length == 3)
                    {
                        // SHA512_224 is a tricky one, treat the output
                        //  as 32-bit integers for simplicity
                        Span<uint> X32 = MemoryMarshal.Cast<byte, uint>(to);

                        // Assign the result to the highest significant bits of d
                        X32[6] = 0xffffffff;
                        X32[6] &= (uint)d;
                    }

                    if (X.Length > 3)
                    {
                        // 3 for SHA512_256, SHA-384 and SHA-512
                        X[3] = d;
                    }

                    if (X.Length > 4)
                    {
                        // 4 and 5 for both SHA-384 and SHA-512
                        X[4] = e;
                        X[5] = f;
                    }

                    if (X.Length == 8)
                    {
                        // 6 and 7 for SHA-512
                        X[6] = g;
                        X[7] = h;
                    }

                    return;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(to), to.Length, "Span must be 28, 32, 48 or 64 bytes long");
        }
    }

    /// <summary>
    /// Revert the byte order for the block registers
    /// </summary>
    public void Revert()
    {
        a = (ulong)IPAddress.HostToNetworkOrder((long)a);
        b = (ulong)IPAddress.HostToNetworkOrder((long)b);
        c = (ulong)IPAddress.HostToNetworkOrder((long)c);
        d = (ulong)IPAddress.HostToNetworkOrder((long)d);
        e = (ulong)IPAddress.HostToNetworkOrder((long)e);
        f = (ulong)IPAddress.HostToNetworkOrder((long)f);
        g = (ulong)IPAddress.HostToNetworkOrder((long)g);
        h = (ulong)IPAddress.HostToNetworkOrder((long)h);
    }

    /// <summary>
    /// Size of structure in memory when treated as a collection of ulong values
    /// </summary>
    public const int TypeUlongSz = 8;

    /// <summary>
    /// Size of structure in memory when treated as a collection of bytes
    /// </summary>
    public const int TypeByteSz = TypeUlongSz * sizeof(ulong);

    #region Public access to named register fields
    [FieldOffset(0)]
    public ulong a;

    [FieldOffset(8)]
    public ulong b;

    [FieldOffset(16)]
    public ulong c;

    [FieldOffset(24)]
    public ulong d;

    [FieldOffset(32)]
    public ulong e;

    [FieldOffset(40)]
    public ulong f;

    [FieldOffset(48)]
    public ulong g;

    [FieldOffset(56)]
    public ulong h;
    #endregion
}
