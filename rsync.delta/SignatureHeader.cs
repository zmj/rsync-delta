using System;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct SignatureHeader
    {
        public const uint Size = 12;

        public readonly uint Magic; // todo: enum?
        public readonly uint BlockLength;
        public readonly uint StrongHashLength;

        public SignatureHeader(uint blockLength, uint strongHashLength)
        {
            Magic = (uint)SignatureFormat.Blake2b;
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }

        public SignatureHeader(ReadOnlySpan<byte> buffer)
        {
            ValidateLength(buffer);
            Magic = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            BlockLength = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4));
            StrongHashLength = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8));
        }

        public void WriteTo(Span<byte> buffer)
        {
            ValidateLength(buffer);
            BinaryPrimitives.WriteUInt32BigEndian(buffer, Magic);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4), BlockLength);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(8), StrongHashLength);
        }

        private static void ValidateLength(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < Size)
            {
                throw new ArgumentException($"Expected a buffer of at least {Size} bytes");
            }
        }
    }
}