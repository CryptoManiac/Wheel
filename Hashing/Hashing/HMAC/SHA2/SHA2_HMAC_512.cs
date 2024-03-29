﻿using System.Runtime.InteropServices;
using Wheel.Hashing.SHA.SHA512;
using Wheel.Hashing.SHA.SHA512.Internal;

namespace Wheel.Hashing.HMAC.SHA2
{
	[StructLayout(LayoutKind.Explicit)]
    internal struct SHA512Base_HMAC : IMac
    {
        [FieldOffset(0)]
        private SHA512Base ctx_inside = new();

        [FieldOffset(SHA512Base.TypeByteSz)]
        private SHA512Base ctx_outside = new();

        // For key pre-hashing
        [FieldOffset(SHA512Base.TypeByteSz * 2)]
        private SHA512Base ctx_prehasher;

        #region For Reset()
        [FieldOffset(SHA512Base.TypeByteSz * 3)]
        private SHA512Base ctx_inside_reinit = new();

        [FieldOffset(SHA512Base.TypeByteSz * 4)]
        private SHA512Base ctx_outside_reinit = new();
        #endregion

        [FieldOffset(SHA512Base.TypeByteSz * 5)]
        private bool initialized;

        public int HashSz => ctx_inside.HashSz;

        public SHA512Base_HMAC(in InternalSHA512State constants, int outSz)
        {
            ctx_inside = new(constants, outSz);
            ctx_outside = ctx_inside;
            ctx_prehasher = ctx_inside;
            ctx_inside_reinit = ctx_inside;
            ctx_outside_reinit = ctx_inside;
            initialized = false;
        }

        public void Init(ReadOnlySpan<byte> key)
        {
            int keySz;

            Span<byte> key_used = stackalloc byte[InternalSHA512Block.TypeByteSz];
            Span<byte> block_opad = stackalloc byte[InternalSHA512Block.TypeByteSz];
            Span<byte> block_ipad = stackalloc byte[InternalSHA512Block.TypeByteSz];

            if (key.Length == InternalSHA512Block.TypeByteSz)
            {
                key.CopyTo(key_used);
                keySz = InternalSHA512Block.TypeByteSz;
            }
            else
            {
                if (key.Length > InternalSHA512Block.TypeByteSz)
                {
                    keySz = ctx_prehasher.HashSz;
                    ctx_prehasher.Reset();
                    ctx_prehasher.Update(key);
                    ctx_prehasher.Digest(key_used.Slice(0, ctx_prehasher.HashSz));
                }
                else
                {
                    key.CopyTo(key_used);
                    keySz = key.Length;
                }

                int fill = InternalSHA512Block.TypeByteSz - keySz;

                block_ipad.Slice(keySz).Fill(0x36);
                block_opad.Slice(keySz).Fill(0x5c);
            }

            for (int i = 0; i < keySz; ++i)
            {
                block_ipad[i] = (byte)(key_used[i] ^ 0x36);
                block_opad[i] = (byte)(key_used[i] ^ 0x5c);
            }

            ctx_inside.Reset();
            ctx_outside.Reset();
            ctx_inside_reinit.Reset();
            ctx_outside_reinit.Reset();

            ctx_inside.Update(block_ipad);
            ctx_outside.Update(block_opad);

            // for Reset()
            ctx_inside_reinit.Update(block_ipad);
            ctx_outside_reinit.Update(block_opad);

            // Allow update/digest calls
            initialized = true;
        }

        public void Update(ReadOnlySpan<byte> message)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Trying to update the uninitialized HMAC structure. Please call the Init() method first.");
            }
            ctx_inside.Update(message);
        }

        public void Digest(Span<byte> mac)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Trying to get a Digest() result from the uninitialized HMAC structure. Please call the Init() method first.");
            }
            Span<byte> mac_temp = stackalloc byte[ctx_inside.HashSz];
            ctx_inside.Digest(mac_temp);
            ctx_outside.Update(mac_temp);
            ctx_outside.Digest(mac_temp);
            mac_temp.Slice(0, mac.Length).CopyTo(mac);
            Reset();
        }

        public void Reset()
        {
            ctx_inside = ctx_inside_reinit;
            ctx_outside = ctx_outside_reinit;
        }

        public void Dispose()
        {
            initialized = false;
            ctx_inside.Reset();
            ctx_outside.Reset();
            ctx_inside_reinit.Reset();
            ctx_outside_reinit.Reset();
        }

        public IMac Clone()
        {
            return this;
        }
    }

    public struct HMAC_SHA512_224 : IMac
    {
        private SHA512Base_HMAC ctx;

        public HMAC_SHA512_224()
        {
            ctx = new(InternalSHA512Constants.init_state_224, 28);
        }

        public int HashSz => ctx.HashSz;
        public void Digest(Span<byte> hash) => ctx.Digest(hash);
        public void Reset() => ctx.Reset();
        public void Init(ReadOnlySpan<byte> key) => ctx.Init(key);
        public void Update(ReadOnlySpan<byte> input) => ctx.Update(input);
        public void Dispose() => ctx.Dispose();
        public IMac Clone() => ctx.Clone();
    }

    public struct HMAC_SHA512_256 : IMac
    {
        private SHA512Base_HMAC ctx;

        public HMAC_SHA512_256()
        {
            ctx = new(InternalSHA512Constants.init_state_256, 32);
        }

        public int HashSz => ctx.HashSz;
        public void Digest(Span<byte> hash) => ctx.Digest(hash);
        public void Reset() => ctx.Reset();
        public void Init(ReadOnlySpan<byte> key) => ctx.Init(key);
        public void Update(ReadOnlySpan<byte> input) => ctx.Update(input);
        public void Dispose() => ctx.Dispose();
        public IMac Clone() => ctx.Clone();
    }

    public struct HMAC_SHA384 : IMac
    {
        private SHA512Base_HMAC ctx;

        public HMAC_SHA384()
        {
            ctx = new(InternalSHA512Constants.init_state_384, 48);
        }

        public int HashSz => ctx.HashSz;
        public void Digest(Span<byte> hash) => ctx.Digest(hash);
        public void Reset() => ctx.Reset();
        public void Init(ReadOnlySpan<byte> key) => ctx.Init(key);
        public void Update(ReadOnlySpan<byte> input) => ctx.Update(input);
        public void Dispose() => ctx.Dispose();
        public IMac Clone() => ctx.Clone();
    }

    public struct HMAC_SHA512 : IMac
    {
        private SHA512Base_HMAC ctx;

        public HMAC_SHA512()
        {
            ctx = new(InternalSHA512Constants.init_state_512, 64);
        }

        public int HashSz => ctx.HashSz;
        public void Digest(Span<byte> hash) => ctx.Digest(hash);
        public void Reset() => ctx.Reset();
        public void Init(ReadOnlySpan<byte> key) => ctx.Init(key);
        public void Update(ReadOnlySpan<byte> input) => ctx.Update(input);
        public void Dispose() => ctx.Dispose();
        public IMac Clone() => ctx.Clone();
    }
}
