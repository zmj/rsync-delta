using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
                return buffer.FirstSpan();
            }
            buffer.CopyTo(valueLengthBuffer);
            return valueLengthBuffer;
        }

        public static byte LastByte(this in ReadOnlySequence<byte> sequence)
        {
            Debug.Assert(sequence.Length > 0);
            if (sequence.IsSingleSegment)
            {
                var span = sequence.FirstSpan();
                return span[span.Length - 1];
            }
            byte last = default;
            foreach (var memory in sequence)
            {
                var span = memory.Span;
                if (!span.IsEmpty)
                {
                    last = span[span.Length - 1];
                }
            }
            return last;
        }

        public static byte FirstByte(this in ReadOnlySequence<byte> sequence)
        {
            Debug.Assert(sequence.Length > 0);
            if (sequence.IsSingleSegment)
            {
                return sequence.FirstSpan()[0];
            }
            foreach (var memory in sequence)
            {
                var span = memory.Span;
                if (!span.IsEmpty)
                {
                    return span[0];
                }
            }
            Debug.Assert(false, "unreachable");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> FirstSpan(
            this in ReadOnlySequence<byte> sequence)
        {
#if !NETSTANDARD2_0
            return sequence.FirstSpan;
#else
            return sequence.First.Span;
#endif
        }
    }
}