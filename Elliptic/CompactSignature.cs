﻿using Wheel.Crypto.Elliptic.Internal.VeryLongInt;

namespace Wheel.Crypto.Elliptic
{
    /// <summary>
    /// Compact signature value pair
    /// </summary>
    public struct CompactSignature : ISignature
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
                fixed (ulong* ptr = &signature_data[0])
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
        public CompactSignature()
        {
        }

        /// <summary>
        /// Construct the empty signature for given curve
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public CompactSignature(ECCurve curve)
        {
            Init(curve);
        }

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
        public CompactSignature(ECCurve curve, ReadOnlySpan<byte> bytes) : this(curve)
        {
            if (!Parse(bytes))
            {
                throw new InvalidDataException("Invalid signature format");
            }
        }

        /// <summary>
        /// Write signature data in compact format
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

            int reqSz = lenS + lenR;
            if (encoded.Length >= reqSz)
            {
                VLI.NativeToBytes(encoded.Slice(0, lenR), lenR, r);
                VLI.NativeToBytes(encoded.Slice(lenR, lenS), lenS, s);
            }
            return reqSz;
        }

        /// <summary>
        /// Parse input and construct signature from its contents
        /// </summary>
        /// <param name="encoded"></param>
        /// <returns>True on success</returns>
        public bool Parse(ReadOnlySpan<byte> encoded)
        {
            if (!Configured)
            {
                throw new InvalidOperationException("No curve settings");
            }

            byte lenR = (byte)curve.NUM_BYTES;
            byte lenS = (byte)curve.NUM_BYTES;

            int reqLen = lenS + lenR;

            if (encoded.Length != reqLen)
            {
                // Must be long enough to contain two encoded integer values
                return false;
            }

            // Decode R and S values
            VLI.BytesToNative(r, encoded.Slice(0, lenR), lenR);
            VLI.BytesToNative(s, encoded.Slice(lenR, lenS), lenS);

            return true;
        }
    }
}
