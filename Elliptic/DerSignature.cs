﻿using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Wheel.Crypto.Elliptic.Internal.VeryLongInt;

namespace Wheel.Crypto.Elliptic
{
    /// <summary>
    /// DER encapsulated signature value pair
    /// </summary>
    public struct DERSignature : ISignature
    {
        /// <summary>
        /// ECC implementation to use
        /// </summary>
        public ECCurve curve { get; private set; }

        /// <summary>
        /// R part of the signature
        /// </summary>
        public readonly unsafe Span<ulong> r
        {
            get
            {
                fixed(ulong* ptr = &signature_data[0])
                {
                    return new Span<ulong>(ptr, curve.NUM_WORDS);
                }
            }
        }

        /// <summary>
        /// S part of the signature
        /// </summary>
        public readonly unsafe Span<ulong> s
        {
            get
            {
                fixed (ulong* ptr = &signature_data[curve.NUM_WORDS])
                {
                    return new Span<ulong>(ptr, curve.NUM_WORDS);
                }
            }
        }

        /// <summary>
        /// Was this instance initialized with curve settings or not
        /// </summary>
        public readonly bool Configured => !r.IsEmpty && !s.IsEmpty;

        /// <summary>
        /// The r and s are sliced from this hidden array.
        /// </summary>
        private unsafe fixed ulong signature_data[2 * VLI.ECC_MAX_WORDS];

        /// <summary>
        /// Construct the unconfigured signature
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public DERSignature()
        {
        }

        /// <summary>
        /// Construct the empty signature for given curve
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public DERSignature(ECCurve curve)
        {
            Init(curve);
        }

        /// <summary>
        /// Initialize for required curve settings
        /// </summary>
        /// <param name="curve">ECC curve implementation</param>
        public unsafe void Init(ECCurve curve)
        {
            this.curve = curve;
            // Sanity check constraint
            if (curve.NUM_WORDS > VLI.ECC_MAX_WORDS)
            {
                throw new SystemException("The configured curve point coordinate size is unexpectedly big");
            }

            unsafe
            {
                fixed (ulong* ptr = &signature_data[0])
                {
                    new Span<ulong>(ptr, 2 * VLI.ECC_MAX_WORDS).Clear();
                }
            }
        }

        /// <summary>
        /// Create instance and parse provided data
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public DERSignature(ECCurve curve, ReadOnlySpan<byte> bytes) : this(curve)
        {
            if (!Parse(bytes))
            {
                throw new InvalidDataException("Provided DER signature is not valid");
            }
        }

        /// <summary>
        /// Write signature data in DER format
        /// </summary>
        /// <param name="encoded"></param>
        /// <returns>Number of bytes written/to write</returns>
        public readonly int Encode(Span<byte> encoded)
        {
            if (!Configured)
            {
                throw new InvalidOperationException("No curve settings");
            }

            byte lenR = (byte)curve.NUM_BYTES;
            byte lenS = (byte)curve.NUM_BYTES;

            int reqSz = 6 + lenS + lenR;
            if (encoded.Length >= reqSz)
            {
                // Fill the DER encoded signature skeleton:

                // Sequence tag
                encoded[0] = 0x30;
                // Total data length
                encoded[1] = (byte)(4 + lenS + lenR);
                // Integer tag for R
                encoded[2] = 0x02;
                // R length prefix
                encoded[3] = lenR;
                // Integer tag for S
                encoded[4 + lenR] = 0x02;
                // S length prefix
                encoded[5 + lenR] = lenS;

                // Encode the R and S values
                VLI.NativeToBytes(encoded.Slice(4, lenR), lenR, r);
                VLI.NativeToBytes(encoded.Slice(6 + lenR, lenS), lenS, s);
            }
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
            if (!Configured)
            {
                throw new InvalidOperationException("No curve settings");
            }

            int rpos, rlen, spos, slen;
            int pos = 0;
            int lenbyte;

            int inputlen = encoded.Length;
            int num_bytes = curve.NUM_BYTES;

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
            lenbyte = encoded[pos++];
            if ((lenbyte & 0x80) != 0)
            {
                lenbyte -= 0x80;
                if (lenbyte > inputlen - pos)
                {
                    return false;
                }
                pos += lenbyte;
            }

            // Integer tag byte for R
            if (pos == inputlen || encoded[pos] != 0x02)
            {
                return false;
            }
            pos++;

            /* Integer length for R */
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

            if (rlen > num_bytes || slen > num_bytes)
            {
                // Overflow
                return false;
            }

            // Decode R and S values
            VLI.BytesToNative(r, encoded.Slice(rpos, rlen), rlen);
            VLI.BytesToNative(s, encoded.Slice(spos, slen), slen);

            return true;
        }
    }
}
