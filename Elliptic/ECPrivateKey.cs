﻿using System;
using System.Runtime.InteropServices;
using Wheel.Crypto.Elliptic.Internal;
using Wheel.Crypto.Elliptic.Internal.VeryLongInt;
using Wheel.Hashing;
using Wheel.Hashing.Derivation;
using Wheel.Hashing.HMAC;

namespace Wheel.Crypto.Elliptic
{
    /// <summary>
    /// Encapsulated ECC private key
    /// </summary>
    public struct ECPrivateKey
    {
        /// <summary>
        /// The secret key funcions are using slices that are being made from this hidden array.
        /// </summary>
        internal unsafe fixed ulong private_key_data[VLI.ECC_MAX_WORDS];

        /// <summary>
        /// ECC implementation to use
        /// </summary>
        public readonly ECCurve curve;

        /// <summary>
        /// Access to the private scalar data
        /// </summary>
        private readonly unsafe Span<ulong> secret_x
        {
            get
            {
                fixed (ulong* ptr = &private_key_data[0])
                {
                    return new Span<ulong>(ptr, curve.NUM_WORDS);
                }
            }
        }

        /// <summary>
        /// Construct the empty key
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public ECPrivateKey(in ECCurve curve)
        {
            this.curve = curve;

            // Init with zeros
            unsafe
            {
                fixed (ulong* ptr = &private_key_data[0])
                {
                    new Span<ulong>(ptr, VLI.ECC_MAX_WORDS).Clear();
                }
            }
        }

        /// <summary>
        /// Construct the the new private key instance from the given serialized scalar
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public ECPrivateKey(in ECCurve curve, ReadOnlySpan<byte> scalar) : this(curve)
        {
            if (!Parse(scalar))
            {
                throw new InvalidDataException("Provided scalar is not valid");
            }
        }

        /// <summary>
        /// Construct the the new private key instance from the given serialized scalar
        /// </summary>
        /// <param name="curve">ECC implementation</param>
        public ECPrivateKey(in ECCurve curve, ReadOnlySpan<ulong> native_scalar) : this(curve)
        {
            if (!Wrap(native_scalar))
            {
                throw new InvalidDataException("Provided native scalar is not valid");
            }
        }

        /// <summary>
        /// Does this instance contain a valid key or not
        /// </summary>
        public unsafe readonly bool IsValid
        {
            get
            {
                if (VLI.IsZero(secret_x, curve.NUM_WORDS))
                {
                    return false;
                }
                VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble
                bool result = VLI.Cmp(curve.n, secret_x, curve.NUM_N_WORDS) == 1;
                VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble
                return result;
            }
        }

        /// <summary>
        /// Erase object state
        /// </summary>
        public void Reset()
        {
            VLI.Clear(secret_x, curve.NUM_WORDS);
        }

        /// <summary>
        /// Dump the native point data
        /// </summary>
        /// <param name="native"></param>
        /// <returns>True if point is valid and copying has been successful</returns>
        public readonly bool UnWrap(Span<ulong> native_out)
        {
            if (!IsValid || native_out.Length != curve.NUM_WORDS)
            {
                return false;
            }

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble
            secret_x.CopyTo(native_out);
            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble
            return true;
        }

        /// <summary>
        /// Set native secret data to given value
        /// </summary>
        /// <param name="native_in"></param>
        /// <returns>True if secret is valid and copying has been successful</returns>
        public bool Wrap(ReadOnlySpan<ulong> native_in)
        {
            if (native_in.Length != curve.NUM_N_WORDS)
            {
                return false;
            }

            // Make sure the private key is in the range [1, n-1].
            if (VLI.IsZero(native_in, curve.NUM_N_WORDS) || VLI.Cmp(curve.n, native_in, curve.NUM_N_WORDS) != 1)
            {
                return false;
            }

            VLI.Set(secret_x, native_in, curve.NUM_N_WORDS);
            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble
            return true;
        }

        /// <summary>
        /// Check to see if a serialized private key is valid.
        /// Note that you are not required to check for a valid private key before using any other functions.
        /// </summary>
        /// <param name="private_key">The private key to check.</param>
        /// <returns>True if the private key is valid.</returns>
        public static bool IsValidPrivateKey(ReadOnlySpan<byte> private_key, ECCurve curve)
        {
            ECPrivateKey pk = new(curve);
            return pk.Parse(private_key);
        }

        /// <summary>
        /// Serialize the native key into big endian number
        /// </summary>
        /// <param name="secret_scalar"></param>
        /// <returns>True if successful and this key is valid</returns>
        public readonly bool Serialize(Span<byte> secret_scalar)
        {
            if (!IsValid || secret_scalar.Length != curve.NUM_BYTES)
            {
                return false;
            }

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble
            VLI.NativeToBytes(secret_scalar, curve.NUM_N_BYTES, secret_x);
            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble

            return true;
        }

        /// <summary>
        /// Try to init using the provided bytes
        /// </summary>
        /// <param name="private_key">Serialized scalar data</param>
        /// <returns>True if successful</returns>
        public bool Parse(ReadOnlySpan<byte> private_key)
        {
            Reset();
            Span<ulong> native_key = stackalloc ulong[VLI.ECC_MAX_WORDS];
            VLI.BytesToNative(native_key, private_key, curve.NUM_N_BYTES);
            bool result = Wrap(native_key);
            VLI.Clear(native_key, curve.NUM_WORDS);
            return result;
        }

        /// <summary>
        /// Compute the corresponding public key for a private key.
        /// </summary>
        /// <param name="public_key">Will be filled in with the corresponding public key</param>
        /// <param name="private_key"> The private key to compute the public key for</param>
        /// <returns>True if the key was computed successfully, False if an error occurred.</returns>
        public readonly bool ComputePublicKey(out ECPublicKey public_key)
        {
            public_key = new(curve);

            if (!IsValid)
            {
                return false;
            }

            Span<ulong> _public = stackalloc ulong[VLI.ECC_MAX_WORDS * 2];

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble
            bool computed = ECCPoint.ComputePublicPoint(curve, _public, secret_x);
            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble

            // Compute public key.
            if (!computed)
            {
                return false;
            }

            return public_key.Wrap(_public);
        }

        /// <summary>
        /// Private key tweak by scalar
        /// </summary>
        /// <param name="result"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public readonly bool KeyTweak(ref ECPrivateKey result, ReadOnlySpan<byte> scalar)
        {
            if (!IsValid)
            {
                return false;
            }

            Span<ulong> _result = stackalloc ulong[VLI.ECC_MAX_WORDS];
            Span<ulong> _scalar = stackalloc ulong[VLI.ECC_MAX_WORDS];

            VLI.BytesToNative(_scalar, scalar, curve.NUM_N_BYTES);

            // Make sure that scalar is in the range [1, n-1]
            if (VLI.IsZero(_scalar, curve.NUM_N_WORDS))
            {
                return false;
            }

            if (VLI.Cmp(curve.n, _scalar, curve.NUM_N_WORDS) != 1)
            {
                return false;
            }

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble

            // Apply scalar addition
            //   r = (a + scalar) % n
            VLI.ModAdd(_result, secret_x, _scalar, curve.n, curve.NUM_N_WORDS);

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble

            // Try to wrap the resulting key data
            return result.Wrap(_result);
        }

        /// <summary>
        /// Generate an ECDSA signature for a given hash value, with the user-provided provided K
        /// </summary>
        /// <param name="r">Will be filled in with the signature value</param>
        /// <param name="s">Will be filled in with the signature value</param>
        /// <param name="message_hash">The hash of the message to sign</param>
        /// <param name="K">Random secret</param>
        /// <param name="K_shadow">A "shadow" of the random secret</param>
        /// <returns></returns>
        private readonly bool SignWithK(Span<ulong> r, Span<ulong> s, ReadOnlySpan<byte> message_hash, ReadOnlySpan<ulong> K, ReadOnlySpan<ulong> K_shadow)
        {
            Span<ulong> p = stackalloc ulong[VLI.ECC_MAX_WORDS * 2];
            Span<ulong> tmp = stackalloc ulong[VLI.ECC_MAX_WORDS];
            VLI.Picker<ulong> k2 = new(tmp, s);

            int num_words = curve.NUM_WORDS;
            int num_bytes = curve.NUM_BYTES;
            int num_n_words = curve.NUM_N_WORDS;
            int num_n_bits = curve.NUM_N_BITS;

            ulong carry;

            // Make a local copy of K for in-place modification
            Span<ulong> k = stackalloc ulong[VLI.ECC_MAX_WORDS];
            VLI.Set(k, K, num_words);

            // Make sure 0 < k < curve_n 
            if (VLI.IsZero(k, num_words) || VLI.Cmp(curve.n, k, num_n_words) != 1)
            {
                throw new InvalidDataException("The secret k value does not meet the requirements");
            }

            carry = ECCUtil.RegularizeK(curve, k, tmp, s);
            ECCPoint.PointMul(curve, p, curve.G, k2[Convert.ToUInt64(!Convert.ToBoolean(carry))], num_n_bits + 1);
            if (VLI.IsZero(p, num_words))
            {
                return false;
            }

            // Prevent side channel analysis of VLI_Arithmetic.ModInv() to determine
            //   bits of k / the private key by premultiplying by a random number
            VLI.Set(tmp, K_shadow, num_n_words);
            VLI.ModMult(k, k, tmp, curve.n, num_n_words); // k' = rand * k
            VLI.ModInv(k, k, curve.n, num_n_words);       // k = 1 / k'
            VLI.ModMult(k, k, tmp, curve.n, num_n_words); // k = 1 / k

            VLI.Set(r, p, num_words); // store r

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble
            VLI.Set(tmp, secret_x, curve.NUM_N_WORDS); // tmp = private key
            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble

            s[num_n_words - 1] = 0;
            VLI.Set(s, p, num_words);
            VLI.ModMult(s, tmp, s, curve.n, num_n_words); // s = r*d

            ECCUtil.BitsToInt(curve, tmp, message_hash, message_hash.Length);
            VLI.ModAdd(s, tmp, s, curve.n, num_n_words); // s = e + r*d 
            VLI.ModMult(s, s, k, curve.n, num_n_words);  // s = (e + r*d) / k 
            if (VLI.NumBits(s, num_n_words) > num_bytes * 8)
            {
                return false;
            }

            if (VLI.Cmp(s, curve.half_n, num_words) == 1)
            {
                // Apply Low-S rule to signature
                VLI.Sub(s, curve.n, s, num_words); // s = n - s 
            }

            return true;
        }

        /// <summary>
        /// Generate an ECDSA signature for a given hash value, using a deterministic algorithm
        /// 
        /// Usage: Compute a hash of the data you wish to sign and pass it to this function along with your private key and entropy bytes. The entropy bytes argument may be set to empty array if you don't need this feature.
        /// </summary>
        /// <param name="r">Will be filled in with the signature value</param>
        /// <param name="s">Will be filled in with the signature value</param>
        /// <param name="message_hash">The hash of the message to sign</param>
        /// <returns></returns>
        private readonly bool SignDeterministic<HMAC_IMPL>(Span<ulong> r, Span<ulong> s, ReadOnlySpan<byte> message_hash) where HMAC_IMPL : unmanaged, IMac
        {
            // Secret K will be written here
            Span<ulong> K = stackalloc ulong[VLI.ECC_MAX_WORDS];
            Span<ulong> K_shadow = stackalloc ulong[VLI.ECC_MAX_WORDS];

            // Will retry until succeed
            for (int i = 1; i != int.MaxValue; ++i)
            {
                GenerateK<HMAC_IMPL>(ref K, message_hash, i);
                GenerateK<HMAC_IMPL>(ref K_shadow, message_hash, -i);

                // Try to sign
                if (SignWithK(r, s, message_hash, K, K_shadow))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generate an ECDSA signature for a given hash value, using a deterministic algorithm
        /// 
        /// Usage: Compute a hash of the data you wish to sign and pass it to this function along with your private key and entropy bytes. The entropy bytes argument may be set to empty array if you don't need this feature.
        /// </summary>
        /// <param name="signature">Will be filled in with the signature value. Curve settings will be overwritten.</param>
        /// <param name="message_hash">The hash of the message to sign</param>
        /// <returns></returns>
        public readonly bool Sign<HMAC_IMPL, SIGNATURE_FORMAT_IMPL>(ref SIGNATURE_FORMAT_IMPL signature, ReadOnlySpan<byte> message_hash) where HMAC_IMPL : unmanaged, IMac where SIGNATURE_FORMAT_IMPL : struct, ISignature
        {
            signature.Init(curve);
            return SignDeterministic<HMAC_IMPL>(signature.r, signature.s, message_hash);
        }

        /// <summary>
        /// Call GenerateSecret using this key as the seed and entropy argument as the personalization string
        /// </summary>
        /// <typeparam name="HMAC_IMPL">HMAC implementation to use</typeparam>
        /// <param name="result">New secret key will be placed here</param>
        /// <param name="entropy">Entropy bytes (random or some user input, not necessarily secret)</param>
        /// <param name="sequence">Key sequence (to generate the different keys for the same source key and entropy bytes array pair)</param>
        /// <param name="expand_iterations">Number of PBKDF2 iterations for the seed and personalize bytes expansion</param>
        /// <exception cref="InvalidOperationException">Thrown when called on the either empty or invalid ECPrivateKey instance</exception>
        public readonly void DeriveHMAC<HMAC_IMPL>(out ECPrivateKey result, ReadOnlySpan<byte> entropy, int sequence, int expand_iterations) where HMAC_IMPL : unmanaged, IMac
        {
            // We're using our private key as secret seed and the entropy is
            //  being used as the personalization string
            Span<byte> seed = stackalloc byte[curve.NUM_N_BYTES];
            if (!Serialize(seed))
            {
                throw new InvalidOperationException("Trying to derive from the invalid private key");
            }

            GenerateSecret<HMAC_IMPL>(curve, out result, seed, entropy, sequence, expand_iterations);
            seed.Clear();
        }

        /// <summary>
        /// Deterministic derivation of a new private key
        /// </summary>
        /// <typeparam name="HMAC_IMPL"></typeparam>
        /// <param name="curve">ECC implementation to use</param>
        /// <param name="result">Resulting key to be filled</param>
        /// <param name="seed">Secret seed</param>
        /// <param name="personalization">Personalization (to generate the different keys for the same seed)</param>
        /// <param name="sequence">Key sequence (to generate the different keys for the same seed and personalization bytes array pair)</param>
        /// <param name="expand_iterations">Number of PBKDF2 iterations for the seed and personalize bytes expansion, 4096 or more is recommended for the new secret keys</param>
        public static void GenerateSecret<HMAC_IMPL>(ECCurve curve, out ECPrivateKey result, ReadOnlySpan<byte> seed, ReadOnlySpan<byte> personalization, int sequence, int expand_iterations) where HMAC_IMPL : unmanaged, IMac
        {
            // See 3..2 of the RFC 6979 to get what is going on here
            // We're not following it to the letter, but our algorithm is very similar

            HMAC_IMPL hmac = new();
            Span<byte> separator_00 = stackalloc byte[1] { 0x00 };
            Span<byte> separator_01 = stackalloc byte[1] { 0x01 };

            Span<byte> sequence_data = stackalloc byte[sizeof(int)];
            Span<byte> expanded_seed = stackalloc byte[hmac.HashSz];
            Span<byte> expanded_personalization = stackalloc byte[hmac.HashSz];

            // Convert sequence to bytes
            MemoryMarshal.Cast<byte, int>(sequence_data)[0] = sequence;

            // Expand the secret seed and personalization string bytes
            PBKDF2.Derive<HMAC_IMPL>(expanded_seed, seed, sequence_data, expand_iterations);
            PBKDF2.Derive<HMAC_IMPL>(expanded_personalization, sequence_data, seed, expand_iterations);

            // Allocate buffer for HMAC results
            Span<byte> K = stackalloc byte[hmac.HashSz];
            Span<byte> V = stackalloc byte[hmac.HashSz];

            // B
            K.Fill(0); // K = 00 00 00 ..

            // C
            V.Fill(0x01); // V = 01 01 01 ..

            // D
            hmac.Init(K); // K = HMAC_K(V || 00 || expanded_seed || 00 || expanded_personalization)
            hmac.Update(V);
            hmac.Update(separator_00);
            hmac.Update(expanded_seed);
            hmac.Update(separator_00);
            hmac.Update(expanded_personalization);
            hmac.Digest(K);

            // E
            hmac.Init(K); // V = HMAC_K(V)
            hmac.Update(V);
            hmac.Digest(V);

            // F
            hmac.Init(K); // K = HMAC_K(V || 01 || expanded_seed || 01 || expanded_personalization)
            hmac.Update(V);
            hmac.Update(separator_01);
            hmac.Update(expanded_seed);
            hmac.Update(separator_01);
            hmac.Update(expanded_personalization);
            hmac.Digest(K);

            // G
            hmac.Init(K); // V = HMAC_K(V)
            hmac.Update(V);
            hmac.Digest(V);

            // H
            int secret_byte_index = 0;
            Span<byte> secret_data = stackalloc byte[curve.NUM_N_BYTES];

            while (true)
            {
                // H2
                hmac.Init(K); // V = HMAC_K(V)
                hmac.Update(V);
                hmac.Digest(V);

                // T = T || V
                Span<byte> src = V.Slice(0, Math.Min(V.Length, secret_data.Length - secret_byte_index));
                Span<byte> target = secret_data.Slice(secret_byte_index);
                src.CopyTo(target);
                secret_byte_index += src.Length;

                if (secret_byte_index >= curve.NUM_N_BYTES)
                {
                    if (IsValidPrivateKey(secret_data, curve))
                    {
                        result = new ECPrivateKey(curve, secret_data);
                        secret_data.Clear();
                        return;
                    }

                    // Doesn't meet the curve criteria,
                    // start filling from zero
                    secret_data.Clear();
                    secret_byte_index = 0;
                }

                // H3
                hmac.Init(K);  // K = HMAC_K(V || 00 || expanded_seed || 00 || expanded_personalization)
                hmac.Update(V);
                hmac.Update(separator_00);
                hmac.Update(expanded_seed);
                hmac.Update(separator_00);
                hmac.Update(expanded_personalization);
                hmac.Digest(K);

                hmac.Init(K); // V = HMAC_K(V)
                hmac.Update(V);
                hmac.Digest(V);
            }
        }

        /// <summary>
        /// Generate deterministic K value for signing
        /// </summary>
        /// <param name="result"></param>
        /// <param name="message_hash"></param>
        /// <param name="sequence"></param>
        private readonly void GenerateK<HMAC_IMPL>(ref Span<ulong> result, ReadOnlySpan<byte> message_hash, int sequence) where HMAC_IMPL : unmanaged, IMac
        {
            // The K value requirements are identical to shose for the secret key.
            // This means that any valis secret key is acceptable to be used as K value.

            // 128 iterations are more than enough for our purposes here
            DeriveHMAC<HMAC_IMPL>(out ECPrivateKey pk, message_hash, sequence, 128);

            // The generated private key is used as secret K value
            pk.UnWrap(result);
        }

        /// <summary>
        /// Compute a shared secret given your secret key and someone else's public key.
        ///
        /// Note: It is recommended that you hash the result of Derive() before using it for
        /// symmetric encryption or HMAC.
        /// </summary>
        /// <param name="public_key">The public key of the remote party.</param>
        /// <param name="shared">Will be filled in with the encapsulated shared secret.</param>
        /// <returns>True if the shared secret was generated successfully, False if an error occurred.</returns>
        public readonly bool ECDH(in ECPublicKey public_key, out ECPrivateKey shared)
        {
            // Init an empty secret to fill it later
            shared = new(public_key.curve);

            if (!IsValid)
            {
                return false;
            }

            if (curve != public_key.curve)
            {
                // It doesn't make any sense to use points on non-matching curves
                // This shouldn't ever happen in real life
                throw new InvalidOperationException("Curve configuration mismatch");
            }

            int num_words = public_key.curve.NUM_WORDS;
            int num_bytes = public_key.curve.NUM_BYTES;

            Span<ulong> ecdh_point = stackalloc ulong[VLI.ECC_MAX_WORDS * 2];
            if (!public_key.UnWrap(ecdh_point))
            {
                // Doesn't make any sense to
                // use invalid points
                return false;
            }

            Span<ulong> secret_scalar_x = stackalloc ulong[VLI.ECC_MAX_WORDS];
            Span<ulong> temp_scalar_k = stackalloc ulong[VLI.ECC_MAX_WORDS];

            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Unscramble
            VLI.Set(secret_scalar_x, secret_x, num_words);
            VLI.XorWith(secret_x, curve.scrambleKey, curve.NUM_WORDS); // Scramble

            VLI.Picker<ulong> p2 = new(secret_scalar_x, temp_scalar_k);
            ulong carry;

            // Regularize the bitcount for the private key so that attackers
            // cannot use a side channel attack to learn the number of leading zeros.
            carry = ECCUtil.RegularizeK(curve, secret_scalar_x, secret_scalar_x, temp_scalar_k);

            ECCPoint.PointMul(curve, ecdh_point, ecdh_point, p2[Convert.ToUInt64(!Convert.ToBoolean(carry))], curve.NUM_N_BITS + 1);

            // Will fail if the point is zero
            bool result = shared.Wrap(ecdh_point.Slice(0, num_words));

            // Clear the temporary vars
            VLI.Clear(ecdh_point, 2 * num_words);
            VLI.Clear(secret_scalar_x, num_words);
            VLI.Clear(temp_scalar_k, num_words);

            return result;
        }

        /// <summary>
        /// Encode the secret into big endian format and calculate
        ///  its hash using the provided IHasher implementation.
        /// May be used to hash the ECDH derived shared keys.
        /// </summary>
        /// <typeparam name="HASHER_IMPL">Hasher to use</typeparam>
        /// <param name="secret_hash"></param>
        /// <returns>True if successful</returns>
        public readonly bool CalculateKeyHash<HASHER_IMPL>(Span<byte> secret_hash) where HASHER_IMPL : unmanaged, IHasher
        {
            HASHER_IMPL hasher = new();
            Span<byte> secret_bytes = stackalloc byte[curve.NUM_BYTES];
            if (secret_hash.Length == hasher.HashSz && Serialize(secret_bytes))
            {
                hasher.Update(secret_bytes);
                secret_bytes.Clear();
                hasher.Digest(secret_hash);
                return true;
            }
            return false;
        }
    }
}

