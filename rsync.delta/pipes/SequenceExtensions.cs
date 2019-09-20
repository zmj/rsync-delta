using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Rsync.Delta.Pipes
{
    internal static class SequenceExtensions
    {
        public static byte ReadByte(this ref ReadOnlySequence<byte> sequence)
        {
            Span<byte> buffer = stackalloc byte[1];
            var value = sequence.ReadN(buffer);
            return value[0];
        }

        public static ushort ReadUShortBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            Span<byte> buffer = stackalloc byte[2];
            var value = sequence.ReadN(buffer);
            return BinaryPrimitives.ReadUInt16BigEndian(value);
        }

        public static int ReadIntBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            Span<byte> buffer = stackalloc byte[4];
            var value = sequence.ReadN(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(value);
        }

        public static uint ReadUIntBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            Span<byte> buffer = stackalloc byte[4];
            var value = sequence.ReadN(buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(value);
        }

        public static ulong ReadULongBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            Span<byte> buffer = stackalloc byte[8];
            var value = sequence.ReadN(buffer);
            return BinaryPrimitives.ReadUInt64BigEndian(value);
        }

        public static ReadOnlySpan<byte> ReadN(
            this ref ReadOnlySequence<byte> sequence,
            Span<byte> valueLengthBuffer)
        {
            int valueLength = valueLengthBuffer.Length;
            Debug.Assert(sequence.Length >= valueLength);
            var buffer = sequence.Slice(sequence.Start, valueLength);
            sequence = sequence.Slice(buffer.End);
            if (buffer.IsSingleSegment)
            {
                return buffer.First.Span;
            }
            buffer.CopyTo(valueLengthBuffer);
            return valueLengthBuffer;
        }

        public static byte LastByte(this in ReadOnlySequence<byte> sequence)
        {
            Debug.Assert(sequence.Length > 0);
            if (sequence.IsSingleSegment)
            {
                var buffer = sequence.First.Span;
                return buffer[buffer.Length - 1];
            }
            byte last = default;
            foreach (var memory in sequence)
            {
                if (!memory.IsEmpty)
                {
                    last = memory.Span[memory.Length - 1];
                }
            }
            return last;
        }

        public static byte FirstByte(this in ReadOnlySequence<byte> data)
        {
            Debug.Assert(data.Length > 0);
            var buffer = data.Slice(data.Start, 1);
            if (buffer.IsSingleSegment)
            {
                return buffer.First.Span[0];
            }
            foreach (var memory in data)
            {
                if (!memory.IsEmpty)
                {
                    return memory.Span[0];
                }
            }
            Debug.Assert(false, "unreachable");
            return 0;
        }
    }
}