using System;
using System.Buffers;
using System.Buffers.Binary;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct DeltaHeader : IWritable, IReadable<DeltaHeader>
    {
        public int Size => 4;
        public int MaxSize => Size;
        public int MinSize => Size;

        public const DeltaFormat Format = DeltaFormat.Librsync;

        public DeltaHeader(ref ReadOnlySequence<byte> buffer)
        {
            var format = (DeltaFormat)buffer.ReadUIntBigEndian();
            if (format != DeltaFormat.Librsync)
            {
                throw new FormatException(nameof(DeltaHeader));
            }
        }

        public void WriteTo(Span<byte> buffer) =>
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Format);

        public DeltaHeader? ReadFrom(ref ReadOnlySequence<byte> data) =>
            new DeltaHeader(ref data);
    }

    internal enum DeltaFormat : uint
    {
        Librsync = 0x72730236,
    }
}
