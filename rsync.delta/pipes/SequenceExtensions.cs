using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Rsync.Delta.Models;

namespace Rsync.Delta.Pipes
{
    internal static class SequenceExtensions
    {
        public static T? TryRead<T>(this in ReadOnlySequence<byte> sequence)
            where T : struct, IReadable2<T>
        {
            T t = default;
            long seqLen = sequence.Length;
            if (seqLen < t.MinSize)
            {
                return null;
            }
            int maxSize = t.MaxSize;
            int len = seqLen > maxSize ? maxSize : (int)seqLen;
            return sequence.TryGetSpan(len, out var span) ?
                t.TryReadFrom(span) :
                t.TryReadFrom(sequence.CopyTo(stackalloc byte[len]));
        }

        public static T? TryRead<T, Options>(
            this in ReadOnlySequence<byte> sequence,
            Options options)
            where T : struct, IReadable2<T, Options>
        {
            T t = default;
            long seqLen = sequence.Length;
            if (seqLen < t.MinSize(options))
            {
                return null;
            }
            int maxSize = t.MaxSize(options);
            int len = seqLen > maxSize ? maxSize : (int)seqLen;
            return sequence.TryGetSpan(len, out var span) ?
                t.TryReadFrom(span, options) :
                t.TryReadFrom(
                    sequence.CopyTo(stackalloc byte[len]),
                    options);
        }

        public static byte ReadByte(this ref ReadOnlySequence<byte> sequence)
        {
            byte value = sequence.FirstByte();
            sequence = sequence.Slice(1);
            return value;
        }

        public static ushort ReadUShortBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            const int length = 2;
            ushort value = sequence.TryGetSpan(length, out var span) ?
                BinaryPrimitives.ReadUInt16BigEndian(span) :
                BinaryPrimitives.ReadUInt16BigEndian(sequence.CopyTo(stackalloc byte[length]));
            sequence = sequence.Slice(length);
            return value;
        }

        public static int ReadIntBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            const int length = 4;
            int value = sequence.TryGetSpan(length, out var span) ?
                BinaryPrimitives.ReadInt32BigEndian(span) :
                BinaryPrimitives.ReadInt32BigEndian(sequence.CopyTo(stackalloc byte[length]));
            sequence = sequence.Slice(length);
            return value;
        }

        public static uint ReadUIntBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            const int length = 4;
            uint value = sequence.TryGetSpan(length, out var span) ?
                BinaryPrimitives.ReadUInt32BigEndian(span) :
                BinaryPrimitives.ReadUInt32BigEndian(sequence.CopyTo(stackalloc byte[length]));
            sequence = sequence.Slice(length);
            return value;
        }

        public static ulong ReadULongBigEndian(
            this ref ReadOnlySequence<byte> sequence)
        {
            const int length = 8;
            ulong value = sequence.TryGetSpan(length, out var span) ?
                BinaryPrimitives.ReadUInt64BigEndian(span) :
                BinaryPrimitives.ReadUInt64BigEndian(sequence.CopyTo(stackalloc byte[length]));
            sequence = sequence.Slice(length);
            return value;
        }

        public static bool TryGetSpan(
            this in ReadOnlySequence<byte> sequence,
            int spanLength,
            out ReadOnlySpan<byte> span)
        {
            Debug.Assert(sequence.Length >= spanLength);
            var firstSpan = sequence.FirstSpan();
            if (firstSpan.Length >= spanLength)
            {
                span = firstSpan.Slice(0, spanLength);
                return true;
            }
            span = default;
            return false;
        }

        public static ReadOnlySpan<byte> CopyTo(
            this in ReadOnlySequence<byte> sequence,
            Span<byte> span)
        {
            Debug.Assert(sequence.Length >= span.Length);
            BuffersExtensions.CopyTo(sequence.Slice(0, span.Length), span);
            return span;
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
            var firstSpan = sequence.FirstSpan();
            if (!firstSpan.IsEmpty)
            {
                return firstSpan[0];
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
