﻿namespace Wheel.Crypto.Elliptic
{
	public interface ISignature
	{
        /// <summary>
        /// ECC implementation to use
        /// </summary>
        public ECCurve curve { get; }

        /// <summary>
        /// R part of the signature
        /// </summary>
        public Span<ulong> r { get; }

        /// <summary>
        /// S part of the signature
        /// </summary>
        public Span<ulong> s { get; }

        /// <summary>
        /// Was this instance initialized with curve settings or not
        /// </summary>
        public bool Configured { get; }

        /// <summary>
        /// Initialize for required curve settings
        /// </summary>
        /// <param name="curve">ECC curve implementation</param>
        public void Init(ECCurve curve);

        /// <summary>
        /// Write signature data in current format
        /// </summary>
        /// <param name="encoded"></param>
        /// <returns>Number of bytes written/to write</returns>
        public int Encode(Span<byte> encoded);

        /// <summary>
        /// Parse input and construct signature from its contents
        /// </summary>
        /// <param name="encoded"></param>
        /// <returns>True on success</returns>
        public bool Parse(ReadOnlySpan<byte> encoded);
    }
}

