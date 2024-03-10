﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wheel.Crypto.Hashing.SHA.SHA256.Internal
{
    /// <summary>
    /// Access to individual block bytes through index operator
    /// </summary>
	[StructLayout(LayoutKind.Explicit)]
    public unsafe struct InternalSHA256BlockBytes
    {
        /// <summary>
        /// Index access to individual registers
        /// </summary>
        /// <param name="key">Byte field index [0 .. 63]</param>
        /// <returns>Word value</returns>
        public byte this[uint key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => GetRegisterByte(key);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetRegisterByte(key, value);
        }

        #region Byte access logic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly byte GetRegisterByte(uint index)
        {
            if (index >= TypeByteSz)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. " + TypeByteSz + ") range");
            }
            return data[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetRegisterByte(uint index, byte value)
        {
            if (index >= TypeByteSz)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. " + TypeByteSz + ") range");
            }
            data[index] = value;
        }
        #endregion

        /// <summary>
        /// Size of structure in memory when treated as a collection of bytes
        /// </summary>
        static public readonly int TypeByteSz = sizeof(InternalSHA256BlockBytes);

        [FieldOffset(0)]
        private fixed byte data[64];
    }

    /// <summary>
    /// Represents the block data for the 256-bit family of SHA functions
    /// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct InternalSHA256Block
	{
        /// <summary>
        /// Instantiate as a copy of the other block
        /// </summary>
        /// <param name="round">Other block</param>
        public unsafe InternalSHA256Block(in InternalSHA256Block block)
        {
            fixed (void* source = &block)
            {
                fixed (void* target = &this)
                {
                    new Span<byte>(source, TypeByteSz).CopyTo(new Span<byte>(target, TypeByteSz));
                }
            }
        }

        /// <summary>
        /// Reset some sequence of bytes to zero
        /// </summary>
        /// <param name="begin">Where to begin</param>
        /// <param name="sz">How many bytes to erase</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public unsafe void Wipe(uint begin, uint sz)
        {
            uint byteSz = (uint)TypeByteSz;

            // Begin index must have a sane value
            if (begin >= byteSz)
            {
                throw new ArgumentOutOfRangeException(nameof(begin), begin, "begin index must be within [0 .. " + byteSz + ") range");
            }

            // Maximum size is a distance between the
            //  beginning and the vector size
            uint maxSz = byteSz - begin;

            if (sz > maxSz)
            {
                throw new ArgumentOutOfRangeException(nameof(sz), sz, "sz must be within [0 .. " + maxSz + "] range");
            }

            fixed (void* ptr = &this)
            {
                new Span<byte>((byte*)ptr + begin, (int)sz).Clear();
            }
        }

        /// <summary>
        /// Overwrite the part of value with a sequence of bytes
        /// </summary>
        /// <param name="bytes">Bytes to write</param>
        /// <param name="targetIndex">Offset to write them from the beginning of this vector</param>
        public unsafe void Write(Span<byte> bytes, uint targetIndex)
        {
            uint byteSz = (uint)TypeByteSz;

            // Target index must have a sane value
            if (targetIndex >= byteSz)
            {
                throw new ArgumentOutOfRangeException(nameof(targetIndex), targetIndex, "targetIndex index must be within [0 .. " + byteSz + ") range");
            }

            // Maximum size is a distance between the
            //  beginning and the vector size
            uint limit = byteSz - targetIndex;

            if (bytes.Length > limit)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, "byte sequence is too long");
            }

            fixed (void* ptr = &this)
            {
                Span<byte> target = new((byte*)ptr + targetIndex, bytes.Length);
                bytes.CopyTo(target);
            }
        }

        /// <summary>
        /// Index access to individual registers
        /// </summary>
        /// <param name="key">Field index [0 .. 7]</param>
        /// <returns>Word value</returns>
        public uint this[uint key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => GetRegisterUint(key);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetRegisterUint(key, value);
        }

        #region Register access logic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly uint GetRegisterUint(uint index)
        {
            if (index >= TypeUintSz)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. " + TypeUintSz + ") range");
            }
            return registers[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetRegisterUint(uint index, uint value)
        {
            if (index >= TypeUintSz)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within [0 .. " + TypeUintSz + ") range");
            }
            registers[index] = value;
        }
        #endregion

        /// <summary>
        /// Set to zero
        /// </summary>
        public unsafe void Reset()
        {
            fixed (void* ptr = &this)
            {
                new Span<byte>(ptr, TypeByteSz).Clear();
            }
        }

        /// <summary>
        /// Size of structure in memory when treated as a collection of uint values
        /// </summary>
        static public readonly int TypeUintSz = sizeof(InternalSHA256Block) / 4;

        /// <summary>
        /// Size of structure in memory when treated as a collection of bytes
        /// </summary>
        static public readonly int TypeByteSz = sizeof(InternalSHA256Block);

        /// <summary>
        /// Fixed size buffer for registers
        /// </summary>
        [FieldOffset(0)]
        private fixed uint registers[16];

        /// <summary>
        /// Public indexed access to the individual block bytes
        /// </summary>
        [FieldOffset(0)]
        public InternalSHA256BlockBytes bytes;

        /// <summary>
        /// Special case: Public access to the last double word (64-bit) for length addition
        /// </summary>
        [FieldOffset(56)]
        public ulong lastDWord;
    }
}
