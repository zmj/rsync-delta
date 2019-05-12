using System;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct BlockSignature
    {
        public static uint Size(uint strongHashLength) =>
            strongHashLength + 4;

        public readonly uint RollingHash;
        public readonly byte[] StrongHash; // optimize

        public BlockSignature(uint rollingHash, byte[] strongHash)
        {
            RollingHash = rollingHash;
            StrongHash = strongHash;
        }

        public BlockSignature(ReadOnlySpan<byte> buffer, uint strongHashLength)
        {
            ValidateLength(buffer, strongHashLength);
            RollingHash = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            StrongHash = new byte[strongHashLength];
            buffer.Slice(4).CopyTo(StrongHash);
        }

        public void WriteTo(Span<byte> buffer)
        {
            ValidateLength(buffer, (uint)StrongHash.Length); // check length at input
            BinaryPrimitives.WriteUInt32BigEndian(buffer, RollingHash);
            StrongHash.CopyTo(buffer.Slice(4));
        }

        private static void ValidateLength(ReadOnlySpan<byte> buffer, uint strongHashLength)
        {
            var size = Size(strongHashLength);
            if (buffer.Length < size)
            {
                throw new ArgumentException($"Expected a buffer of at least {size} bytes");
            }
        }
    }
}