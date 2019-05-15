using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct BlockSignature
    {
        public static uint Size(uint strongHashLength) =>
            strongHashLength + 4;

        public readonly uint RollingHash;
        public readonly Memory<byte> StrongHash; // rent from matcher

        public BlockSignature(uint rollingHash, Memory<byte> strongHash)
        {
            RollingHash = rollingHash;
            StrongHash = strongHash;
        }

        public BlockSignature(SequenceReader<byte> reader, uint strongHashLength)
        {
            var strongHash = new byte[strongHashLength];
            if (reader.TryReadBigEndian(out int rollingHash) &&
                reader.TryCopyTo(strongHash)) // todo: validate == length
            {
                RollingHash = (uint)rollingHash;
                StrongHash = strongHash;
            }
            else
            {
                throw new FormatException(nameof(BlockSignature));
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, RollingHash);
            StrongHash.Span.CopyTo(buffer.Slice(4));
        }
    }
}