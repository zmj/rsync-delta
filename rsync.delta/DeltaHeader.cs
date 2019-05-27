using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct DeltaHeader
    {
        public const ushort Size = 4;

        public const DeltaFormat Format = DeltaFormat.Default;

        public DeltaHeader(ref ReadOnlySequence<byte> buffer)
        {
            var format = (DeltaFormat)buffer.ReadUIntBigEndian();
            if (format != DeltaFormat.Default)
            {
                throw new FormatException(nameof(DeltaHeader));
            }
        }

        public void WriteTo(Span<byte> buffer) =>
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Format);
    }

    internal enum DeltaFormat : uint
    {
        Default = 0x72730236,
    }
}