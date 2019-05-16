using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct SignatureOptions
    {
        public const ushort Size = 8;

        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);

        public readonly uint BlockLength;
        public readonly uint StrongHashLength;

        public SignatureOptions(SequenceReader<byte> reader)
        {
            if (reader.TryReadBigEndian(out int blockLength) &&
                reader.TryReadBigEndian(out int strongHashLength))
            {
                BlockLength = (uint)blockLength;
                StrongHashLength = (uint)strongHashLength;
            }
            else
            {
                throw new FormatException(nameof(SignatureOptions));
            }
        }

        public SignatureOptions(uint blockLength, uint strongHashLength)
        {
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }

        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, BlockLength);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4), StrongHashLength);
        }
    }
}