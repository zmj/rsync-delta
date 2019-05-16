using System;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct CopyCommand
    {
        private const byte _baseCommand = 0x45;

        private readonly LongRange _range;

        public CopyCommand(LongRange range) => _range = range;

        public int Size => 1 + ByteSize(_range.Start) + ByteSize(_range.Length);

        public void WriteTo(Span<byte> buffer)
        {
            buffer[0] = GetCommand(_range);
            buffer = buffer.Slice(1);
            buffer = WriteValue(_range.Start, buffer);
            WriteValue(_range.Length, buffer);
        }

        // static tryread?

        private static byte GetCommand(LongRange range) =>
            (byte)(_baseCommand +
                (4 * ByteSizeOffset(range.Start)) +
                ByteSizeOffset(range.Length));

        private static int ByteSizeOffset(ulong value) =>
            value switch
            {
                var _ when value <= byte.MaxValue => 0,
                var _ when value <= ushort.MaxValue => 1,
                var _ when value <= uint.MaxValue => 2,
                _ => 3,
            };

        private static int ByteSize(ulong value) =>
            value switch
            {
                var _ when value <= byte.MaxValue => 1,
                var _ when value <= ushort.MaxValue => 2,
                var _ when value <= uint.MaxValue => 4,
                _ => 8,
            };

        private static Span<byte> WriteValue(ulong value, Span<byte> buffer)
        {
            if (value <= byte.MaxValue)
            {
                buffer[0] = (byte)value;
                return buffer.Slice(1);
            }
            else if (value <= ushort.MaxValue)
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)value);
                return buffer.Slice(2);
            }
            else if (value <= uint.MaxValue)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)value);
                return buffer.Slice(4);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
                return buffer.Slice(8);
            }
        }
    }
}