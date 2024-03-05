﻿using System.Runtime.InteropServices;
using Wheel.Crypto.Miscellaneous.Support;

namespace Wheel.Crypto.Primitives.WordVectors
{
    [StructLayout(LayoutKind.Explicit)]
    public struct WordVec16
	{
        public WordVec16()
        {
        }

        public void SetWords(uint w00, uint w01, uint w02, uint w03, uint w04, uint w05, uint w06, uint w07, uint w08, uint w09, uint w10, uint w11, uint w12, uint w13, uint w14, uint w15)
        {
            this.w00 = w00;
            this.w01 = w01;
            this.w02 = w02;
            this.w03 = w03;
            this.w04 = w04;
            this.w05 = w05;
            this.w06 = w06;
            this.w07 = w07;
            this.w08 = w08;
            this.w09 = w09;
            this.w10 = w10;
            this.w11 = w11;
            this.w12 = w12;
            this.w13 = w13;
            this.w14 = w14;
            this.w15 = w15;
        }

        /// <summary>
        /// Set to zeros
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < 16; i++)
            {
                this[i] = 0;
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
        public uint this[int key]
        {
            readonly get => GetWord(key);
            set => SetWord(key, value);
        }

        private readonly uint GetWord(int index)
        {
            switch (index)
            {
                case 0: return w00;
                case 1: return w01;
                case 2: return w02;
                case 3: return w03;
                case 4: return w04;
                case 5: return w05;
                case 6: return w06;
                case 7: return w07;
                case 8: return w08;
                case 9: return w09;
                case 10: return w10;
                case 11: return w11;
                case 12: return w12;
                case 13: return w13;
                case 14: return w14;
                case 15: return w15;
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. 15] range");
                    }
            }
        }

        private uint SetWord(int index, uint value)
        {
            switch (index)
            {
                case 0: return w00 = value;
                case 1: return w01 = value;
                case 2: return w02 = value;
                case 3: return w03 = value;
                case 4: return w04 = value;
                case 5: return w05 = value;
                case 6: return w06 = value;
                case 7: return w07 = value;
                case 8: return w08 = value;
                case 9: return w09 = value;
                case 10: return w10 = value;
                case 11: return w11 = value;
                case 12: return w12 = value;
                case 13: return w13 = value;
                case 14: return w14 = value;
                case 15: return w15 = value;
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. 15] range");
                    }
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
                wv[(int)i] = i;
            }

            for (uint i = 0; i < 16; i++)
            {
                if (i != wv[(int)i]) throw new InvalidDataException("WordVec16 fail");
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
