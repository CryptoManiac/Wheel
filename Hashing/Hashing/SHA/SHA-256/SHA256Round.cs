﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wheel.Miscellaneous.Support;

namespace Wheel.Hashing.SHA.SHA256.Internal
{
    /// <summary>
    /// Represents the round context data for the 256-bit family of SHA functions
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct InternalSHA256Round
    {
        /// <summary>
        /// Instantiate from array or a variable number of arguments
        /// </summary>
        /// <param name="uints"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public unsafe InternalSHA256Round(params uint[] uints)
        {
            if (uints.Length != TypeUintSz)
            {
                throw new ArgumentOutOfRangeException(nameof(uints), uints.Length, "Must provide " + TypeUintSz + " arguments exactly");
            }

            fixed (void* source = &uints[0])
            {
                fixed (void* target = &this)
                {
                    new Span<byte>(source, TypeByteSz).CopyTo(new Span<byte>(target, TypeByteSz));
                }
            }
        }

        /// <summary>
        /// Initialize first 16 registers from the provided block and revert them
        /// </summary>
        /// <param name="block">A context to provide 16 registers</param>
        internal InternalSHA256Round(in InternalSHA256Block block)
        {
            SetBlock(block);
            RevertBlock();
        }

        /// <summary>
        /// Set first 16 registers from the provided container
        /// </summary>
        /// <param name="block">A context to provide 16 registers</param>
        private unsafe void SetBlock(in InternalSHA256Block block)
        {
            fixed (void* source = &block)
            {
                fixed (void* target = &this)
                {
                    new Span<byte>(source, InternalSHA256Block.TypeByteSz).CopyTo(new Span<byte>(target, TypeByteSz));
                }
            }
        }

        /// <summary>
        /// Revert the byte order for the first 16 state registers
        /// </summary>
        private unsafe void RevertBlock()
        {
            for (int i = 0; i < InternalSHA256Block.TypeUintSz; ++i)
            {
                Common.REVERT(ref registers[i]);
            }
        }

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
        public const int TypeUintSz = 64;

        // <summary>
        /// Size of structure in memory when treated as a collection of bytes
        /// </summary>
        public const int TypeByteSz = TypeUintSz * sizeof(uint);

        /// <summary>
        /// Fixed size buffer for registers
        /// </summary>
        [FieldOffset(0)]
        internal unsafe fixed uint registers[TypeUintSz];
    }
}
