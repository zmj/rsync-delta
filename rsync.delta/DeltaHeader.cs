using System;
using System.Buffers;
using System.Buffers.Binary;
using Rsync.Delta.Pipes;

namespace Rsync.Delta
{
    internal readonly struct DeltaHeader
    {
        public const ushort Size = 4;

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
    }

    internal enum DeltaFormat : uint
    {
        Librsync = 0x72730236,
    }
}