﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wheel.Crypto.Miscellaneous.Support;

namespace Wheel.Crypto.Primitives.WordVectors
{
    [StructLayout(LayoutKind.Explicit)]
    public struct WordVec16
	{
        public WordVec16(params uint[] words)
        {
            SetWords(words);
        }

        public unsafe void SetWords(WordVec16 wv16)
        {
            fixed (void* target = &this)
            {
                Buffer.MemoryCopy(&wv16, target, sizeof(uint) * 16, sizeof(uint) * 16);
            }
        }

        public unsafe void SetWords(params uint[] words)
        {
            if (words.Length != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(words), words.Length, "Must provide 16 words exactly");
            }

            fixed (void* target = &this)
            {
                fixed (void* src = &words[0])
                {
                    Buffer.MemoryCopy(src, target, sizeof(uint) * 16, sizeof(uint) * 16);
                }
            }
        }

        public unsafe void GetWords(Span<uint> to)
        {
            fixed(void* ptr = &this)
            {
                new Span<uint>(ptr, 16).CopyTo(to);
            }
        }

        /// <summary>
        /// Set to zero
        /// </summary>
        public unsafe void Reset()
        {
            fixed (void* ptr = &this)
            {
                Unsafe.InitBlockUnaligned(ptr, 0, sizeof(uint) * 16);
            }
        }

        /// <summary>
        /// Reverse byte order for all words
        /// </summary>
        public void RevertWords()
        {
            w00 = Common.REVERT(w00);
            w01 = Common.REVERT(w01);
            w02 = Common.REVERT(w02);
            w03 = Common.REVERT(w03);
            w04 = Common.REVERT(w04);
            w05 = Common.REVERT(w05);
            w06 = Common.REVERT(w06);
            w07 = Common.REVERT(w07);
            w08 = Common.REVERT(w08);
            w09 = Common.REVERT(w09);
            w10 = Common.REVERT(w10);
            w11 = Common.REVERT(w11);
            w12 = Common.REVERT(w12);
            w13 = Common.REVERT(w13);
            w14 = Common.REVERT(w14);
            w15 = Common.REVERT(w15);
        }

        /// <summary>
        /// Index access to individual word fields
        /// </summary>
        /// <param name="key">Byte field index [0 .. 15]</param>
        /// <returns>Word value</returns>
        public uint this[uint key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => GetWord(key);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetWord(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe readonly uint GetWord(uint index)
        {
            if (index > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. 15] range");
            }

            fixed (uint* src = &w00)
            {
                return src[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint SetWord(uint index, uint value)
        {
            if (index > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. 15] range");
            }

            fixed (uint* target = &w00)
            {
                return target[index] = value;
            }
        }

        /// <summary>
        /// Test method
        /// </summary>
        public static void Test()
        {
            WordVec16 wv = new();
            for (uint i = 0; i < 16; i++)
            {
                wv[i] = i;
            }

            for (uint i = 0; i < 16; i++)
            {
                if (i != wv[i]) throw new InvalidDataException("WordVec16 fail");
            }
        }

        #region Individual word fields
        [FieldOffset(0)]
        public uint w00 = 0;
        [FieldOffset(1 * sizeof(uint))]
        public uint w01 = 0;
        [FieldOffset(2 * sizeof(uint))]
        public uint w02 = 0;
        [FieldOffset(3 * sizeof(uint))]
        public uint w03 = 0;

        [FieldOffset(4 * sizeof(uint))]
        public uint w04 = 0;
        [FieldOffset(5 * sizeof(uint))]
        public uint w05 = 0;
        [FieldOffset(6 * sizeof(uint))]
        public uint w06 = 0;
        [FieldOffset(7 * sizeof(uint))]
        public uint w07 = 0;

        [FieldOffset(8 * sizeof(uint))]
        public uint w08 = 0;
        [FieldOffset(9 * sizeof(uint))]
        public uint w09 = 0;
        [FieldOffset(10 * sizeof(uint))]
        public uint w10 = 0;
        [FieldOffset(11 * sizeof(uint))]
        public uint w11 = 0;

        [FieldOffset(12 * sizeof(uint))]
        public uint w12 = 0;
        [FieldOffset(13 * sizeof(uint))]
        public uint w13 = 0;
        [FieldOffset(14 * sizeof(uint))]
        public uint w14 = 0;
        [FieldOffset(15 * sizeof(uint))]
        public uint w15 = 0;
        #endregion
    }
}
