﻿using Wheel.Crypto.Elliptic.EllipticCommon;

namespace Wheel.Crypto.Elliptic.EdDSA;

/// <summary>
/// EdDSA DER encapsulated signature value pair
/// </summary>
public struct DERSignature : IEdSignature
{
    /// <summary>
    /// ECC implementation to use
    /// </summary>
    private readonly EdCurve _curve { get; }

    /// <summary>
    /// Public property for unification purposes
    /// </summary>
    public readonly ICurve curve => _curve;

    /// <summary>
    /// Encoded data size in bytes
    /// </summary>
    public readonly int EncodedSize => GetEncodedSize(_curve);

    /// <summary>
    /// The r and s are sliced from this hidden array.
    /// </summary>
    private unsafe fixed byte signature_data[64];

    /// <summary>
    /// R part of the signature
    /// </summary>
    public readonly unsafe Span<byte> r
    {
        get
        {
            fixed (byte* ptr = &signature_data[0])
            {
                return new Span<byte>(ptr, 32);
            }
        }
    }

    /// <summary>
    /// S part of the signature
    /// </summary>
    public readonly unsafe Span<byte> s
    {
        get
        {
            fixed (byte* ptr = &signature_data[32])
            {
                return new Span<byte>(ptr, 32);
            }
        }
    }

    public DERSignature()
    {
        throw new SystemException("The default constructor should never be called");
    }

    /// <summary>
    /// Construct the empty signature for given curve
    /// </summary>
    /// <param name="curve">ECC implementation</param>
    public DERSignature(EdCurve curve)
    {
        _curve = curve;
        r.Clear();
        s.Clear();
    }

    /// <summary>
    /// Create instance and parse provided data
    /// </summary>
    /// <param name="curve">ECC implementation</param>
    public DERSignature(EdCurve curve, ReadOnlySpan<byte> bytes) : this(curve)
    {
        if (!Parse(bytes))
        {
            throw new InvalidDataException("Provided DER signature is not valid");
        }
    }

    public readonly int Encode(Span<byte> encoded)
    {
        encoded.Clear();

        // Check whether we have 0x7f byte or not to add prefix
        int lenR = r.Length + (r[0] > 0x7F ? 1 : 0);
        int lenS = s.Length + (s[0] > 0x7F ? 1 : 0);

        // Length of R and S and their prefixes
        int seqSz = 4 + lenR + lenS;

        // Actually required size
        int reqSz = 2 + seqSz + (seqSz > 0x7F ? 1 : 0);

        if (encoded.Length >= reqSz)
        {
            int pos = 0;

            // Sequence tag
            encoded[pos++] = 0x30;

            // Sequence length prefix
            if (seqSz > 0x7F)
            {
                // Special case for two byte value
                encoded[pos++] = 1 | 0x80;
            }

            // Sequence length
            encoded[pos++] = (byte)(seqSz & 0xff);

            // Integer tag for R
            encoded[pos++] = 0x02;

            // R length
            encoded[pos++] = (byte) lenR;

            // Negative R prefix
            if (lenR != r.Length)
            {
                encoded[pos++] = 0x00;
            }

            r.CopyTo(encoded.Slice(pos, r.Length));

            pos += r.Length;

            // Integer tag for S
            encoded[pos++] = 0x02;

            // S length
            encoded[pos++] = (byte) lenS;

            // Negative S prefix
            if (lenS != s.Length)
            {
                encoded[pos++] = 0x00;
            }

            s.CopyTo(encoded.Slice(pos, s.Length));
        }

        // Number of bytes written
        return reqSz;
    }

    /// <summary>
    /// Maximum size of encoded signature for a given curve
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    public static int GetEncodedSize(EdCurve curve)
    {
        // Integer tags, integer lengths and prefixes
        int seqSz = 4 + 2 * 32 + 2;

        // Content type tag + content length + sequence length
        int reqSz = 2 + seqSz;
        if (seqSz > 127) ++reqSz;
        return reqSz;
    }

    /// <summary>
    /// Parse DER formatted input and construct signature from its contents
    /// Note: based on parse_der_lax routine from the bitcoin distribution
    /// </summary>
    /// <param name="encoded"></param>
    /// <returns>True on success</returns>
    public bool Parse(ReadOnlySpan<byte> encoded)
    {
        int rpos, rlen, spos, slen;
        int pos = 0;
        int lenbyte;
        int len;

        int inputlen = encoded.Length;

        // Sequence tag byte
        if (pos == inputlen || encoded[pos] != 0x30)
        {
            return false;
        }
        pos++;

        // Sequence length bytes
        if (pos == inputlen)
        {
            return false;
        }
        len = lenbyte = encoded[pos++];

        if ((lenbyte & 0x80) != 0)
        {
            lenbyte -= 0x80;
            if (lenbyte > inputlen - pos)
            {
                return false;
            }
            // Save length for
            //  the format check
            len = encoded[pos];
            pos += lenbyte;
        }

        // Integer tag byte for R
        if (pos == inputlen || encoded[pos] != 0x02)
        {
            return false;
        }
        pos++;

        // Integer length for R
        if (pos == inputlen)
        {
            return false;
        }
        lenbyte = encoded[pos++];
        if (Convert.ToBoolean(lenbyte & 0x80))
        {
            lenbyte -= 0x80;
            if (lenbyte > inputlen - pos)
            {
                return false;
            }
            while (lenbyte > 0 && encoded[pos] == 0)
            {
                pos++;
                lenbyte--;
            }
            if (lenbyte >= 4)
            {
                return false;
            }
            rlen = 0;
            while (lenbyte > 0)
            {
                rlen = (rlen << 8) + encoded[pos];
                pos++;
                lenbyte--;
            }
        }
        else
        {
            rlen = lenbyte;
        }
        if (rlen > inputlen - pos)
        {
            return false;
        }
        rpos = pos;
        pos += rlen;

        // Integer tag byte for S
        if (pos == inputlen || encoded[pos] != 0x02)
        {
            return false;
        }
        pos++;

        // Integer length for S
        if (pos == inputlen)
        {
            return false;
        }
        lenbyte = encoded[pos++];
        if (Convert.ToBoolean(lenbyte & 0x80))
        {
            lenbyte -= 0x80;
            if (lenbyte > inputlen - pos)
            {
                return false;
            }
            while (lenbyte > 0 && encoded[pos] == 0)
            {
                pos++;
                lenbyte--;
            }
            if (lenbyte >= 4)
            {
                return false;
            }
            slen = 0;
            while (lenbyte > 0)
            {
                slen = (slen << 8) + encoded[pos];
                pos++;
                lenbyte--;
            }
        }
        else
        {
            slen = lenbyte;
        }
        if (slen > inputlen - pos)
        {
            return false;
        }
        spos = pos;

        if (len != (4 + slen + rlen) )
        {
            return false;
        }

        // Remove r prefix
        if ((rlen - 1) == 32 && encoded[rpos] == 0x00)
        {
            rpos++;
            rlen--;
        }

        // Remove s prefix
        if ((slen - 1) == 32 && encoded[spos] == 0x00)
        {
            spos++;
            slen--;
        }

        if (rlen > 32 || slen > 32)
        {
            // Overflow
            return false;
        }

        // Decode R and S values
        encoded.Slice(rpos, rlen).CopyTo(r);
        encoded.Slice(spos, slen).CopyTo(s);

        return true;
    }
}
