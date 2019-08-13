using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    public readonly struct SignatureOptions : IWritable
    {
        public int Size => 8;

        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);

        public readonly int BlockLength;
        public readonly int StrongHashLength;

        internal SignatureOptions(ref ReadOnlySequence<byte> buffer)
        {
            BlockLength = buffer.ReadIntBigEndian();
            StrongHashLength = buffer.ReadIntBigEndian();
            Validate();
        }
        
        public SignatureOptions(int blockLength, int strongHashLength)
        {
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
            Validate();
        }

        private void Validate()
        {
            if (BlockLength <= 0) // todo: max blocklen
            {
                throw new ArgumentOutOfRangeException(nameof(BlockLength));
            }
            if (StrongHashLength <= 0 || StrongHashLength > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(StrongHashLength));
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            BinaryPrimitives.WriteInt32BigEndian(buffer, BlockLength);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), StrongHashLength);
        }
    }
}