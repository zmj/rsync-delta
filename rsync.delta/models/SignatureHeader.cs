using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct SignatureHeader :
        IWritable, IReadable<SignatureHeader>, IReadable2<SignatureHeader>
    {
        private const int _magicBase = 0x72730100;

        private const int _size = 12;
        public int Size => _size;
        public int MaxSize => _size;
        public int MinSize => _size;

        public readonly SignatureOptions Options;

        public SignatureHeader(SignatureOptions options) => Options = options;

        public SignatureHeader(ref ReadOnlySequence<byte> buffer)
        {
            int magic = buffer.ReadIntBigEndian();
            if ((magic & 0xFFFFFF00) != _magicBase)
            {
                throw new FormatException($"unknown magic: {magic}");
            }
            Options = new SignatureOptions(
                blockLength: buffer.ReadIntBigEndian(),
                strongHashLength: buffer.ReadIntBigEndian(),
                rollingHash: (RollingHashAlgorithm)(magic & 0xF0),
                strongHash: (StrongHashAlgorithm)(magic & 0xF));
        }

        public void WriteTo(Span<byte> buffer)
        {
            int magic = _magicBase | (int)Options.RollingHash | (int)Options.StrongHash;
            BinaryPrimitives.WriteInt32BigEndian(buffer, magic);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), Options.BlockLength);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(8), Options.StrongHashLength);
        }

        public SignatureHeader? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            return new SignatureHeader(ref data);
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
                throw new FormatException($"unknown magic: {magic}");
            }
            header = new SignatureHeader(new SignatureOptions(
                blockLength: BinaryPrimitives.ReadInt32BigEndian(span.Slice(4)),
                strongHashLength: BinaryPrimitives.ReadInt32BigEndian(span.Slice(8)),
                rollingHash: (RollingHashAlgorithm)(magic & 0xF0),
                strongHash: (StrongHashAlgorithm)(magic & 0xF)));
            return OperationStatus.Done;
        }
    }
}
