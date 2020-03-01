using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Rsync.Delta.Models
{
    internal readonly struct SignatureHeader : IWritable, IReadable<SignatureHeader>
    {
        private const int _magicBase = 0x72730100;

        private const int _size = 12;
        public int Size => _size;
        public int MaxSize => _size;
        public int MinSize => _size;

        public readonly SignatureOptions Options;

        public SignatureHeader(SignatureOptions options) => Options = options;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= _size);
            int magic = _magicBase | (int)Options.RollingHashAlgorithm | (int)Options.StrongHash;
            BinaryPrimitives.WriteInt32BigEndian(buffer, magic);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), Options.BlockLength);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(8), Options.StrongHashLength);
        }

        public OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            out SignatureHeader header)
        {
            if (span.Length < _size)
            {
                header = default;
                return OperationStatus.NeedMoreData;
            }
            int magic = BinaryPrimitives.ReadInt32BigEndian(span);
            if ((magic & 0xFFFFFF00) != _magicBase)
            {
                header = default;
                return OperationStatus.InvalidData;
            }
            header = new SignatureHeader(new SignatureOptions(
                blockLength: BinaryPrimitives.ReadInt32BigEndian(span.Slice(4)),
                strongHashLength: BinaryPrimitives.ReadInt32BigEndian(span.Slice(8)),
                rollingHashAlgorithm: (RollingHashAlgorithm)(magic & 0xF0),
                strongHashAlgorithm: (StrongHashAlgorithm)(magic & 0xF)));
            return OperationStatus.Done;
        }
    }
}
