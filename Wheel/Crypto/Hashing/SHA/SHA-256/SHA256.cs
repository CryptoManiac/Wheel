﻿using Wheel.Crypto.Miscellaneous.Support;
using System.Runtime.CompilerServices;
using Wheel.Crypto.Hashing.SHA.SHA256.Internal;

namespace Wheel.Crypto.Hashing.SHA.SHA256
{
    public abstract class SHA256Base : IHasher
    {
        /// <summary>
        /// Current data block length in bytes
        /// </summary>
        protected uint blockLen;

        /// <summary>
        /// Total input length in bits
        /// </summary>
        protected ulong bitLen;

        /// <summary>
        /// Pending block data to transform
        /// </summary>
        protected InternalSHA256Block pendingBlock = new();

        /// <summary>
        /// Current hashing state
        /// </summary>
        protected InternalSHA256State state = new();

        public SHA256Base()
        {
            Reset();
        }

        /// <summary>
        /// Reset to initial state
        /// </summary>
        public void Reset()
        {
            blockLen = 0;
            bitLen = 0;
            pendingBlock.Reset();
            SetInitState();
        }

        protected abstract void SetInitState();
        protected abstract int GetHashLength();

        /// <summary>
        /// Write hash into given byte array
        /// </summary>
        /// <param name="hash">Byte array to write into</param>
        public void Digest(Span<byte> hash)
        {
            if (hash.Length != GetHashLength())
            {
                throw new InvalidOperationException("Target buffer size doesn't match the expected " + GetHashLength() + " bytes");
            }

            Finish();
            state.Store(hash);
            Reset();
        }

        /// <summary>
        /// Get SHA256 hash as a new byte array
        /// </summary>
        /// <returns></returns>
        public byte[] Digest()
        {
            Finish();
            byte[] hash = new byte[GetHashLength()];
            state.Store(hash);
            Reset();
            return hash;
        }

        /// <summary>
        /// Update hasher with new data bytes
        /// </summary>
        /// <param name="input">Input bytes to update hasher with</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Update(byte[] input)
        {
            for (int i = 0; i < input.Length;)
            {
                // How many bytes are left unprocessed
                int remaining = input.Length - i;

                // How many bytes are needed to complete this block
                int needed = 64 - (int)blockLen;

                // Either entire remaining byte stream or merely a needed chunk of it
                Span<byte> toWrite = new(input, i, (remaining < needed) ? remaining : needed);

                // Write data at current index
                pendingBlock.Write(toWrite, blockLen);

                i += toWrite.Length;
                blockLen += (uint)toWrite.Length;

                if (blockLen == 64)
                {
                    // End of the block
                    Transform();
                    bitLen += 512;
                    blockLen = 0;
                }
            }
        }

        protected unsafe void Transform()
        {
            // Initialize with first 16 words filled from the
            // pending block and reverted to big endian
            InternalSHA256Round wordPad = new(pendingBlock);

            // Remaining 48 blocks
            for (uint i = 16; i < 64; ++i)
            {
                wordPad[i] = InternalSHA256Ops.SIG1(wordPad[i - 2]) + wordPad[i - 7] + InternalSHA256Ops.SIG0(wordPad[i - 15]) + wordPad[i - 16];
            }

            InternalSHA256State loc = new(state);

            for (uint i = 0; i < 64; ++i)
            {
                uint t1 = loc.h + InternalSHA256Ops.SIGMA1(loc.e) + InternalSHA256Ops.CHOOSE(loc.e, loc.f, loc.g) + InternalSHA256Constants.K[i] + wordPad[i];
                uint t2 = InternalSHA256Ops.SIGMA0(loc.a) + InternalSHA256Ops.MAJ(loc.a, loc.b, loc.c);

                loc.h = loc.g;
                loc.g = loc.f;
                loc.f = loc.e;
                loc.e = loc.d + t1;
                loc.d = loc.c;
                loc.c = loc.b;
                loc.b = loc.a;
                loc.a = t1 + t2;
            }

            state.Add(loc);
        }

        protected void Finish()
        {

            uint i = blockLen;
            uint end = (blockLen < 56u) ? 56u : 64u;
            pendingBlock.bytes[i++] = 0x80; // Append a bit 1
            pendingBlock.Wipe(i, end - i); // Pad with zeros

            if (blockLen >= 56)
            {
                Transform();
                uint lastWord = pendingBlock[15];
                pendingBlock.Reset();
                pendingBlock[15] = lastWord;
            }

            // Append to the padding the total message's
            // length in bits and transform.
            bitLen += blockLen * 8;
            pendingBlock.lastDWord = Common.REVERT(bitLen);
            Transform();

            // Reverse byte ordering to get final hashing result
            state.Revert();
        }
    }

    public class SHA256 : SHA256Base
	{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int GetHashLength()
        {
            return 32;
        }

        /// <summary>
        /// Set SHA256 constants
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetInitState()
        {
            state.Set(InternalSHA256Constants.init_state_256);
        }

        public static byte[] Hash(byte[] input)
        {
            SHA256 hasher = new();
            hasher.Update(input);
            return hasher.Digest();
        }

        public static void Hash(Span<byte> digest, byte[] input)
        {
            SHA256 hasher = new();
            hasher.Update(input);
            hasher.Digest(digest);
        }
    }

    public class SHA224 : SHA256Base
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int GetHashLength()
        {
            return 28;
        }

        /// <summary>
        /// Set SHA224 constants
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetInitState()
        {
            state.Set(InternalSHA256Constants.init_state_224);
        }

        public static byte[] Hash(byte[] input)
        {
            SHA224 hasher = new();
            hasher.Update(input);
            return hasher.Digest();
        }

        public static void Hash(Span<byte> digest, byte[] input)
        {
            SHA224 hasher = new();
            hasher.Update(input);
            hasher.Digest(digest);
        }
    }
}