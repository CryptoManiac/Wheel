﻿namespace Wheel.Crypto.Elliptic.ECDSA.Internal;

/// <summary>
/// Format conversion operations for very long integers (aka VLI)
/// </summary>
internal static partial class VLI
{
    /// <summary>
    /// Get number of bytes which will be sufficient to encode the required number of bits
    /// </summary>
    /// <param name="num_bits"></param>
    public static int BitsToBytes(int num_bits) => (num_bits + 7) / 8;

    /// <summary>
    /// Get number of words which will be sufficient to encode the required number of bits
    /// </summary>
    /// <param name="num_bits"></param>
    public static int BitsToWords(int num_bits) => (num_bits + ((WORD_SIZE * 8) - 1)) / (WORD_SIZE * 8);

    /// <summary>
    /// Converts big-endian bytes to an integer in the native format.
    /// </summary>
    /// <param name="native"></param>
    /// <param name="bytes"></param>
    /// <param name="num_bytes"></param>
		public static void BytesToNative(Span<ulong> native, ReadOnlySpan<byte> bytes, int num_bytes)
		{
			Clear(native, (num_bytes + (WORD_SIZE - 1)) / WORD_SIZE);
        for (int i = 0; i < num_bytes; ++i)
        {
            int b = num_bytes - 1 - i;
            native[b / WORD_SIZE] |= (ulong)bytes[i] << (8 * (b % WORD_SIZE));
        }
    }

    /// <summary>
    /// Converts an integer in the native format to big-endian bytes.
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="num_bytes"></param>
    /// <param name="native"></param>
    public static void NativeToBytes(Span<byte> bytes, int num_bytes, ReadOnlySpan<ulong> native) {
        for (int i = 0; i < num_bytes; ++i) {
            int b = num_bytes - 1 - i;
            bytes[i] = (byte) (native[b / WORD_SIZE] >> (8 * (b % WORD_SIZE)));
        }
    }
	}

