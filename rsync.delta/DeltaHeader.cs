using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct DeltaHeader
    {
        public const ushort Size = 4;

        public const DeltaFormat Format = DeltaFormat.Default;

        public DeltaHeader(SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        public void WriteTo(Span<byte> buffer) =>
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Format);
    }

    internal enum DeltaFormat : uint
    {
        Default = 0x72730236,
    }
}