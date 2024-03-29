﻿using Wheel.Crypto.Elliptic.Internal.VeryLongInt;

namespace Wheel.Crypto.Elliptic.Internal
{
    /// <summary>
    /// Curve-specific arithmetic functions which are used internally
    /// </summary>
    internal static class ECCUtil
    {
        public static ulong RegularizeK(ECCurve curve, ReadOnlySpan<ulong> k, Span<ulong> k0, Span<ulong> k1)
        {
            int num_n_words = curve.NUM_N_WORDS;
            int num_n_bits = curve.NUM_N_BITS;

            ulong carry = VLI.Add(k0, k, curve.n, num_n_words);
            if (!Convert.ToBoolean(carry))
            {
                carry = Convert.ToUInt64(num_n_bits < (num_n_words * VLI.WORD_SIZE * 8) && VLI.TestBit(k0, num_n_bits));
            }
            VLI.Add(k1, k0, curve.n, num_n_words);
            return carry;
        }

        public static void ModSQRT(Span<ulong> a, ECCurve curve)
        {
            int i;
            Span<ulong> p1 = stackalloc ulong[VLI.ECC_MAX_WORDS];
            Span<ulong> l_result = stackalloc ulong[VLI.ECC_MAX_WORDS];
            p1[0] = l_result[0] = 1;

            int num_words = curve.NUM_WORDS;

            // When curve_secp256k1.p == 3 (mod 4), we can compute
            //   sqrt(a) = a^((curve_secp256k1.p + 1) / 4) (mod curve_secp256k1.p).

            VLI.Add(p1, curve.p, p1, num_words); // p1 = curve_p + 1
            for (i = VLI.NumBits(p1, num_words) - 1; i > 1; --i)
            {
                unsafe
                {
                    curve.ModSquare(l_result, l_result);
                    if (VLI.TestBit(p1, i))
                    {
                        curve.ModMult(l_result, l_result, a);
                    }
                }
            }
            VLI.Set(a, l_result, num_words);
        }

        /// <summary>
        /// Modify (x1, y1) => (x1 * z^2, y1 * z^3)
        /// </summary>
        /// <param name="X1"></param>
        /// <param name="Y1"></param>
        /// <param name="Z"></param>
        public static unsafe void ApplyZ(ECCurve curve, Span<ulong> X1, Span<ulong> Y1, ReadOnlySpan<ulong> Z)
        {
            Span<ulong> t1 = stackalloc ulong[VLI.ECC_MAX_WORDS];
            curve.ModSquare(t1, Z);    // z^2
            curve.ModMult(X1, X1, t1); // x1 * z^2
            curve.ModMult(t1, t1, Z);  // z^3
            curve.ModMult(Y1, Y1, t1); // y1 * z^3
        }

        // P = (x1, y1) => 2P, (x2, y2) => P'
        public static void XYcZ_Initial_Double(ECCurve curve, Span<ulong> X1, Span<ulong> Y1, Span<ulong> X2, Span<ulong> Y2, ReadOnlySpan<ulong> initial_Z)
        {
            Span<ulong> z = stackalloc ulong[VLI.ECC_MAX_WORDS];

            int num_words = curve.NUM_WORDS;

            // Setting Z as it is provided
            VLI.Set(z, initial_Z, num_words);

            VLI.Set(X2, X1, num_words);
            VLI.Set(Y2, Y1, num_words);

            ApplyZ(curve, X1, Y1, z);
            unsafe
            {
                curve.DoubleJacobian(X1, Y1, z);
            }
            ApplyZ(curve, X2, Y2, z);
        }

        // P = (x1, y1) => 2P, (x2, y2) => P'
        public static void XYcZ_Double(ECCurve curve, Span<ulong> X1, Span<ulong> Y1, Span<ulong> X2, Span<ulong> Y2)
        {
            Span<ulong> z = stackalloc ulong[VLI.ECC_MAX_WORDS];

            int num_words = curve.NUM_WORDS;

            // Version without initial_Z
            VLI.Clear(z, num_words);
            z[0] = 1;

            VLI.Set(X2, X1, num_words);
            VLI.Set(Y2, Y1, num_words);

            ApplyZ(curve, X1, Y1, z);
            unsafe
            {
                curve.DoubleJacobian(X1, Y1, z);
            }
            ApplyZ(curve, X2, Y2, z);
        }

        // Input P = (x1, y1, Z), Q = (x2, y2, Z)
        //   Output P' = (x1', y1', Z3), P + Q = (x3, y3, Z3)
        //   or P => P', Q => P + Q
        public static void XYcZ_Add(ECCurve curve, Span<ulong> X1, Span<ulong> Y1, Span<ulong> X2, Span<ulong> Y2)
        {
            // t1 = X1, t2 = Y1, t3 = X2, t4 = Y2
            Span<ulong> t5 = stackalloc ulong[VLI.ECC_MAX_WORDS];

            int num_words = curve.NUM_WORDS;

            VLI.ModSub(t5, X2, X1, curve.p, num_words); // t5 = x2 - x1
            unsafe
            {
                curve.ModSquare(t5, t5);                  // t5 = (x2 - x1)^2 = A
                curve.ModMult(X1, X1, t5);                // t1 = x1*A = B
                curve.ModMult(X2, X2, t5);                // t3 = x2*A = C
            }
            VLI.ModSub(Y2, Y2, Y1, curve.p, num_words); // t4 = y2 - y1
            unsafe
            {
                curve.ModSquare(t5, Y2);                  // t5 = (y2 - y1)^2 = D
            }

            VLI.ModSub(t5, t5, X1, curve.p, num_words); // t5 = D - B
            VLI.ModSub(t5, t5, X2, curve.p, num_words); // t5 = D - B - C = x3
            VLI.ModSub(X2, X2, X1, curve.p, num_words); // t3 = C - B
            unsafe
            {
                curve.ModMult(Y1, Y1, X2);                // t2 = y1*(C - B)
            }
            VLI.ModSub(X2, X1, t5, curve.p, num_words); // t3 = B - x3
            unsafe
            {
                curve.ModMult(Y2, Y2, X2);                // t4 = (y2 - y1)*(B - x3)
            }
            VLI.ModSub(Y2, Y2, Y1, curve.p, num_words); // t4 = y3
            VLI.Set(X2, t5, num_words);
        }

        // Input P = (x1, y1, Z), Q = (x2, y2, Z)
        //   Output P + Q = (x3, y3, Z3), P - Q = (x3', y3', Z3)
        //   or P => P - Q, Q => P + Q
        public static void XYcZ_addC(ECCurve curve, Span<ulong> X1, Span<ulong> Y1, Span<ulong> X2, Span<ulong> Y2)
        {
            // t1 = X1, t2 = Y1, t3 = X2, t4 = Y2
            Span<ulong> t5 = stackalloc ulong[VLI.ECC_MAX_WORDS];
            Span<ulong> t6 = stackalloc ulong[VLI.ECC_MAX_WORDS];
            Span<ulong> t7 = stackalloc ulong[VLI.ECC_MAX_WORDS];

            int num_words = curve.NUM_WORDS;

            VLI.ModSub(t5, X2, X1, curve.p, num_words); // t5 = x2 - x1
            unsafe
            {
                curve.ModSquare(t5, t5);                  // t5 = (x2 - x1)^2 = A
                curve.ModMult(X1, X1, t5);                // t1 = x1*A = B
                curve.ModMult(X2, X2, t5);                // t3 = x2*A = C
            }
            VLI.ModAdd(t5, Y2, Y1, curve.p, num_words); // t5 = y2 + y1
            VLI.ModSub(Y2, Y2, Y1, curve.p, num_words); // t4 = y2 - y1

            VLI.ModSub(t6, X2, X1, curve.p, num_words); // t6 = C - B
            unsafe
            {
                curve.ModMult(Y1, Y1, t6);                // t2 = y1 * (C - B) = E
            }
            VLI.ModAdd(t6, X1, X2, curve.p, num_words); // t6 = B + C
            unsafe
            {
                curve.ModSquare(X2, Y2);                  // t3 = (y2 - y1)^2 = D
            }
            VLI.ModSub(X2, X2, t6, curve.p, num_words); // t3 = D - (B + C) = x3

            VLI.ModSub(t7, X1, X2, curve.p, num_words); // t7 = B - x3
            unsafe
            {
                curve.ModMult(Y2, Y2, t7);                // t4 = (y2 - y1)*(B - x3)
            }
            VLI.ModSub(Y2, Y2, Y1, curve.p, num_words); // t4 = (y2 - y1)*(B - x3) - E = y3
            unsafe
            {
                curve.ModSquare(t7, t5);                  // t7 = (y2 + y1)^2 = F
            }
            VLI.ModSub(t7, t7, t6, curve.p, num_words); // t7 = F - (B + C) = x3'
            VLI.ModSub(t6, t7, X1, curve.p, num_words); // t6 = x3' - B
            unsafe
            {
                curve.ModMult(t6, t6, t5);                // t6 = (y2+y1)*(x3' - B)
            }
            VLI.ModSub(Y1, t6, Y1, curve.p, num_words); // t2 = (y2+y1)*(x3' - B) - E = y3'

            VLI.Set(X1, t7, num_words);
        }

        public static void BitsToInt(ECCurve curve, Span<ulong> native, ReadOnlySpan<byte> bits, int bits_size)
        {
            int num_n_bytes = curve.NUM_N_BYTES;
            int num_n_words = curve.NUM_WORDS;
            int num_n_bits = curve.NUM_N_BITS;

            if (bits_size > num_n_bytes)
            {
                bits_size = num_n_bytes;
            }

            VLI.Clear(native, num_n_words);
            VLI.BytesToNative(native, bits, bits_size);
            if (bits_size * 8 <= num_n_bits)
            {
                return;
            }

            ulong carry = 0;
            int shift = bits_size * 8 - num_n_bits;
            for (int index = num_n_words - 1; index >= 0; --index)
            {
                ulong temp = native[index];
                native[index] = (temp >> shift) | carry;
                carry = temp << (VLI.WORD_BITS - shift);
            }

            /* Reduce mod curve_n */
            if (VLI.CmpUnsafe(curve.n, native, num_n_words) != 1)
            {
                VLI.Sub(native, native, curve.n, num_n_words);
            }
        }
    }
}
