using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct DeltaHeader : IWritable, IReadable<DeltaHeader>
    {
        private const int _size = 4;
        public int Size => _size;
        public int MaxSize => _size;
        public int MinSize => _size;

        private const DeltaFormat _format = DeltaFormat.Librsync;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= _size);
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)_format);
        }            

        public OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            out DeltaHeader value)
        {
            if (span.Length < _size)
            {
                value = default;
                return OperationStatus.NeedMoreData;
            }
            var format = (DeltaFormat)BinaryPrimitives.ReadUInt32BigEndian(span);
            if (format != DeltaFormat.Librsync)
            {
                return OperationStatus.InvalidData;
            }
            return OperationStatus.Done;
        }
    }

    internal enum DeltaFormat : uint
    {
        Librsync = 0x72730236,
    }
}
