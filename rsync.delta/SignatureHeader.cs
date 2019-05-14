using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct SignatureHeader
    {
        public const uint Size = 12;

        public readonly uint Format;
        public readonly uint BlockLength;
        public readonly uint StrongHashLength;

        public SignatureHeader(uint blockLength, uint strongHashLength)
        {
            Format = (uint)SignatureFormat.Blake2b;
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }

        public SignatureHeader(ReadOnlySequence<byte> buffer)
        {
            ValidateLength(buffer.Length);
            var reader = new SequenceReader<byte>(buffer);
            if (reader.TryReadBigEndian(out int format) &&
                reader.TryReadBigEndian(out int blockLength) &&
                reader.TryReadBigEndian(out int strongHashLength))
            {
                Format = (uint)format;
                BlockLength = (uint)blockLength;
                StrongHashLength = (uint)strongHashLength;
            }
            else 
            {
                throw new FormatException(nameof(SignatureHeader));
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            ValidateLength(buffer.Length);
            BinaryPrimitives.WriteUInt32BigEndian(buffer, Format);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4), BlockLength);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(8), StrongHashLength);
        }

        private static void ValidateLength(long bufferLength)
        {
            if (bufferLength < Size)
            {
                throw new ArgumentException($"Expected a buffer of at least {Size} bytes");
            }
        }

        public override string ToString()
        {
            var buffer = new byte[(int)SignatureHeader.Size];
            WriteTo(buffer);
            return BitConverter.ToString(buffer);
        }
    }

    internal enum SignatureFormat : uint
    {
        Blake2b = 0x72730137,
    }
}