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
            if (sequence.First.Length >= valueLength)
            {
                var value = sequence.First.Span.Slice(0, valueLength);
                sequence = sequence.Slice(valueLength);
                return value;
            }
            else
            {
                sequence.Slice(0, valueLength).CopyTo(valueLengthBuffer);
                sequence = sequence.Slice(valueLength);
                return valueLengthBuffer;
            }
        }

        internal static byte LastByte(this in ReadOnlySequence<byte> sequence)
        {
            Debug.Assert(sequence.Length > 0);
            ReadOnlySpan<byte> lastBuffer;
            if (sequence.IsSingleSegment)
            {
                lastBuffer = sequence.First.Span;
            }
            else
            {
                lastBuffer = sequence.Slice(
                    sequence.GetPosition(offset: -1, sequence.End))
                    .First.Span;
            }
            return lastBuffer[lastBuffer.Length - 1];
        }

        internal static byte FirstByte(this in ReadOnlySequence<byte> data)
        {
            Debug.Assert(data.Length > 0);
            return data.First.Span[0];
        }
    }
}